# Steam: the owner's runbook

Every Steamworks step that only the owner can do, in order, with where each value goes. The design is
`docs/superpowers/specs/2026-11-01-steamworks-integration-design.md` and the plan
`docs/superpowers/plans/2026-11-01-steamworks-integration.md` (both in PR #2148); the owner's answers Q1-Q10 are in #2150.
The issue for these steps is #2151.

**Agents never do any of this.** No agent opens an account, pays, creates or reads a secret, types a password, runs
steamcmd or clicks a Steamworks setting. Nothing on this page is a password, a token or a secret value: where one is
needed, the step says where the owner types or pipes it.

Each Steam fact carries the date it was read. Valve's pages change; re-check a fact before acting on it if its date is
old.

## 1. Account, fee and the app

1. Open the Steamworks account as an individual (decision O5, `docs/oss/DECISIONS.md:53`) and fill in the identity, tax
   and bank forms. Valve's Steam Direct page said the paperwork "takes between 15-30 days" (read 14 Oct 2026, quoted in
   `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md:347`).
2. Pay the Steam Direct fee: US$100 per app as read on 14 Oct 2026; **re-check before paying**. A free game does not
   recoup it.
3. Create the app as **a free product**. Steamworks then shows the app ID and lets you add depots.
4. Create three depots, one per OS (section 3), and note the four numbers.
5. Put the numbers in `steam/steam.json` (app ID and the three depot IDs; they are public numbers), in a pull request or by
   giving them to the coordinator. Until then every Steam step refuses by name: `tools/steam_upload.py` (`refuse`, line 71)
   names each `null` ID, and the nightly skips its Steam stage (`tools/nightly.py:783-797`, `steam_ids_missing`). The
   game's own copy of the app ID, `unity/Assets/Steam/SteamAppId.cs` (#2154), is committed with the same number in the
   same pull request; a test pins the two equal (`tools/tests/test_steam_upload.py`,
   `test_the_game_app_id_matches_steam_json`).

(The line numbers in `tools/steam_upload.py`, `tools/release_build.py`, `tools/nightly.py` and
`.github/workflows/steam-upload.yml` on this page are those of the pull request for #2155.)

## 2. The builder account and the CI upload (spec 4.3.1 item 4)

Uploads run only in GitHub Actions (owner answer Q8, "GitHub Actions"), in `.github/workflows/steam-upload.yml`. It needs
two environment secrets that only you create.

1. **The builder account.** Create a separate Steam account for uploads and give it, in Steamworks, only the upload
   permissions. Valve recommends "a dedicated build account" with only "Edit App Metadata" and "Publish App Changes To
   Steam" (upload page, read 14 Oct 2026, `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md:416-418`).
2. **One interactive login, in a temporary folder.** On your own machine, make a new empty folder, install steamcmd there,
   and run it once: `+login <builder account>`, then type the password and the Steam Guard code when steamcmd asks, then
   `+quit`. Use a temporary folder: agents work on the same Mac, and a cached login left in a normal place would let any
   process upload without a password. Valve: the build machine's `config/config.vdf` "is saved and preserved between runs"
   and later logins need no password (upload page, read 1 Nov 2026).
3. **The `steam` environment.** In the repository's Settings > Environments, create `steam` and set its deployment
   branches to `main` only. The workflow's job uses it (`.github/workflows/steam-upload.yml:70`), so a run from any other
   branch never receives the secrets; the job also runs only for your own dispatch on `main` (line 69).
   Do not add a required reviewer to the `steam` environment: uploads need no extra click (owner, 1 Nov 2026, "No extra
   click (Recommended)", `docs/oss/DECISIONS.md`, "Steam"), so each nightly uploads without waiting.
4. **The two secrets.** Add them to the `steam` environment from a terminal, so neither is shown on screen:
   - `gh secret set STEAM_BUILDER_USERNAME --env steam` and type the builder account's name at its prompt.
   - `base64 < <temporary folder>/config/config.vdf | gh secret set STEAM_CONFIG_VDF --env steam`. Where steamcmd puts
     `config/config.vdf` depends on where it was installed; look for it inside the temporary folder (and under
     `~/Steam` on Linux) **(re-check on the first run)**.

   Then delete the temporary folder, so no cached builder login stays on the machine.
5. **Re-provisioning.** When the upload job fails at "Upload with steamcmd (owner's builder account)" with a login error
   (the cached token expired, or Valve rejected the machine), repeat steps 2 and 4. Nobody else touches the secrets. If a
   cached login turns out not to work on GitHub's machines at all, the fallback in spec 4.3.1 item 3 (the Steam Guard
   mobile-authenticator secret as a third secret) is a new decision for you, not something an agent sets up.

What the job does with them, in order (`.github/workflows/steam-upload.yml:77-163`): checks the inputs; downloads the draft
release `steam-<tag>` and checks every zip's SHA-256 against `steam-record.json` (`tools/steam_upload.py:205-225`,
`verify`); checks that the record's commit is on `main`; renders the SteamPipe scripts (`render`, line 99); fetches
steamcmd from Valve; restores the login cache; runs `steamcmd +@NoPromptForPassword 1 +login "$STEAM_BUILDER_USERNAME"
+run_app_build <script> +quit`; always deletes the cache; and, on success, deletes the draft. Only the two login steps see
the secrets. On the first real run a maintainer confirms three facts the workflow's header lists: the steamcmd download
URL, the success line it looks for (`Successfully finished AppID`), and the config path.

## 3. App Admin

Set these in Steamworks App Admin, then publish the App Admin changes.

- **Depots.** One depot per OS: Windows x64, macOS (Universal), Linux x64; set each depot's OS, and leave the architecture
  at "[All Architectures]" with one depot per OS (Depots page, read 14 Oct 2026,
  `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md:407-410`). Each depot uploads its own folder:
  the depot scripts map `<platform>/*` to the depot's root (`steam/depot_build.vdf.in`).
- **Packages.** Add all three depots to the store package **and** to the Dev Comp package, or testers receive no files
  (same pages, read 14 Oct 2026).
- **Branches.** Create `beta` and `nightly`, each with a password. Release tags go to `beta`, every published nightly to
  `nightly` (owner answer Q7, "Also nightlies"). The upload sets a build live on those two only; `default` is never set
  by a script (Valve: "the 'default' branch can not be set live automatically", upload page, read 1 Nov 2026), and
  `tools/steam_upload.py` refuses it (`render`, line 99, `NEVER` at line 51). You set `default` live by hand (section 6).
- **Launch options**, one per OS, the player names `tools/release_build.py` builds (`PLATFORMS`, line 124):
  - Windows: `Statecraft.exe`
  - macOS: `Statecraft.app` (Valve prefers the app bundle: "the app bundle format should be preferred", upload page, read
    14 Oct 2026)
  - Linux: `Statecraft.x86_64`
- **"App Bundles Are Notarized"**: tick it once the macOS depot is signed and notarised (#905). Valve: "Steam will require
  all new macOS Applications to be 64-bit and notarized by Apple" (Platforms page, read 1 Nov 2026).
- **System requirements**: fill them from the first Steam build's measurements; this page does not guess them.
- **Supported languages** (Languages page, read 1 Nov 2026): English with interface, subtitles and full audio; Hebrew with
  interface only, listed as a game-support-only language. Valve lists Hebrew only under "Game Support Only": "These
  languages are not supported in Steamworks APIs", with no API code, so achievement names and descriptions are English
  only, and the game never reads Steam's language. The in-game choice and `-lang` stay as they are
  (`unity/Assets/Scripts/UI/UiLanguage.cs:27-37`).

### Achievements (owner answer Q5, "Five")

Enter exactly these API names; the game unlocks them by name (plan Task 2, #2152: `AchievementRules.ForObjective` and
`AchievementRules.CampaignDone`). The four objectives are the four ids in `content/objectives.yaml` (lines 6, 18, 31, 45).
**The display names and descriptions below were approved by the owner on 1 Nov 2026** ("Approve as drafted",
`docs/oss/DECISIONS.md`, "Steam"); they were written from `content/objectives.yaml`.

| API name | Display name | Description | From |
| --- | --- | --- | --- |
| `OBJ_RAILWAY` | Lay the Haifa-Tel Aviv rail link | Complete the rail corridor between Tel Aviv and Haifa. | `railway`, `content/objectives.yaml:6-17` |
| `OBJ_BURMA_ROAD` | Build the Burma Road corridor | Complete the Burma Road corridor. | `burma_road`, `content/objectives.yaml:18-30` |
| `OBJ_YISHUVIM` | Establish the designated yishuvim | Build all six designated new settlements. | `yishuvim`, `content/objectives.yaml:31-44` |
| `OBJ_ABSORB_IMMIGRANTS` | Absorb the immigrants | Reach a Jewish population of 1,000,000 with the cities' overflow at most 10% and at least 40% of civilians employed. | `absorb_immigrants`, `content/objectives.yaml:45-55` (the 10% and 40% are design values, Q-ABS-2) |
| `CAMPAIGN_DONE` | See it through | Reach the game's last turn. | `GameOverCodes.MvpEnded` (`sim/Statecraft.Sim/GameState.cs:16`), "The game has reached its last turn" (`sim/Statecraft.Sim/PlayerText.cs:22`) |

(The names use a hyphen, not a dash, as every Steam resource name here does. If the design values of
`OBJ_ABSORB_IMMIGRANTS` change before launch, its description changes with them.)

**Icons.** Each achievement needs an unlocked and a locked icon. Their source gets an `ASSETS.yaml` row each, like any
asset, and they must never be adapted from Tal Heuberger's CC BY-NC-ND models (no derivatives; spec section 5). Choosing
or approving the icons is yours.

### Steam Cloud: Auto-Cloud (owner answer Q6)

Valve's Steam Cloud page and Unity 6000.3's `persistentDataPath` page, both read 1 Nov 2026 (spec 4.4). The game writes one
file, `autosave.txt` (`unity/Assets/Scripts/SaveStore.cs:15`), in `<persistentDataPath>/saves`
(`SaveStore.cs:24-25`); the save folder's names come from `companyName: Statecraft` and `productName: Statecraft Israel`
(`unity/ProjectSettings/ProjectSettings.asset:15-16`), which stay frozen: a rename strands local saves and the cloud
mapping.

Set, in this order:
1. **Byte quota per user: 1 MB; number of files per user: 2.** Valve: both must be set before Auto-Cloud works.
2. **Root OS: `[Cross-Platform]`**, which Valve requires when Root Overrides are used.
3. **Root** `WinAppDataLocalLow`, subdirectory `Statecraft/Statecraft Israel/saves`, pattern `autosave.txt`.
4. **Override for macOS:** `MacAppSupport`, subdirectory `unity.Statecraft.Statecraft Israel/saves`.
5. **Override for Linux:** `LinuxHome`, subdirectory `.config/unity3d/Statecraft/Statecraft Israel/saves`. Valve has no
   XDG config root (only `LinuxXdgDataHome`), so this assumes the default `XDG_CONFIG_HOME`; a player who moved it is not
   synced.

Before configuring, read `Application.persistentDataPath` from each first Steam player's log (Unity's macOS path "uses
the Editor path if that directory already exists"). Settings are not synced: quality and UI scale depend on the screen.

## 4. The store page and the Content Survey

- The Coming Soon page from the kit: `docs/steam/coming-soon-kit.md` and `docs/steam/store-page-draft.md` (#424).
- The Content Survey: the draft answers are in `docs/steam/coming-soon-kit.md` section 5; re-check them against the first
  Steam build before submitting. Answering it is yours.
- Privacy: `docs/steam/privacy.md` is the statement for the store page's support fields; it names Unity's engine
  reporting, which stays on (owner, 1 Nov 2026, "Name them").

## 5. Uploads

- **A release** (to `beta`): `docs/RELEASING.md`, "Steam builds", step 5. The maintainer builds with
  `tools/release_build.py --steam` and runs `tools/steam_upload.py stage`; the job uploads.
- **A nightly** (to `nightly`): automatic after each published web nightly (`docs/RELEASING.md`, "Nightly builds",
  the `steam` stage), once the IDs are in `steam/steam.json`.
- Testers on Steam, for each OS that ships: `docs/steam/test-checklist.md`.

## 6. Setting a build live on `default`, and the calendar

Set a build live on `default` in App Admin only when the store page, the Content Survey and Valve's reviews are done, and
only for a `beta` build that passed `docs/steam/test-checklist.md`.

The release calendar (Valve's Steam Direct and Review Process pages, read 14 Oct 2026,
`docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md:349-352`; **re-check before planning dates**): a
30-day wait after paying the fee before the first release; a public Coming Soon page for at least two weeks; the store
page and the build are reviewed separately and "typically take 3-5 business days" each, and Valve asks for at least 7
business days.
