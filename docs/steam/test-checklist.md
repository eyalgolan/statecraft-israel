# Steam: the client test checklist

The manual checks with the Steam client, run by the owner (or a tester the owner adds) on the password-protected `beta`
branch, before a build goes live on `default` (plan `docs/superpowers/plans/2026-11-01-steamworks-integration.md`, Task 10
step 4, in PR #2148; #2157). Run it on each OS that ships. Write the build ID, the OS and the date on the run, and file any
failure as its own issue.

The automated checks come first and are not repeated here: the Steam players pass the `-golden` check (Task 10 step 2),
and `tools/release_build.py --steam` refused any folder holding `steam_appid.txt`.

## Every OS

- [ ] **Install and start from Steam.** The game installs from the `beta` branch and starts from the Steam library.
- [ ] **Started outside Steam, it relaunches through Steam.** Start the installed executable directly: the game quits and
      Steam starts it again (Valve's `RestartAppIfNecessary`; plan Task 6, `SteamStartRule`).
- [ ] **The overlay opens** with Shift+Tab, Steam's default overlay key.
- [ ] **The game's own Shift+Tab still works with the overlay key changed.** Shift+Tab is also the game's own key for moving the
      focus back (spec 4.4, "Overlay"; for example `unity/Assets/Scripts/UI/ScreenNav.cs:333`). With the overlay key at its default, Shift+Tab opens the overlay; change
      the overlay key in Steam's settings and check that Shift+Tab moves the focus back in the game again. The store page
      says this (spec 4.4, "Overlay").
- [ ] **Quit works** from the start screen and from Settings (plan Task 4, #2153).
- [ ] **Hebrew survives a restart and an update.** Choose Hebrew in Settings, quit, start again: the game is in Hebrew.
      Then install the next `beta` build and start: still Hebrew. (The game never reads Steam's language; spec 4.4.)
- [ ] **Each achievement unlocks once.** Use a test account, or reset the achievements first. Play until each of the
      five unlocks (`docs/steam/README.md`, "Achievements"): it shows once, and loading a save that has already earned it,
      or a finished game, shows it no second time.
- [ ] **Auto-Cloud carries a save between machines.** Make an autosave on one OS, quit, start on another OS signed in to
      the same account: the game continues that save (`docs/steam/README.md`, "Steam Cloud").
- [ ] **An older build opens a newer cloud save** without breaking: it warns about the content change, or sets an
      unreadable autosave aside as `autosave.bad.txt` and starts a new game (`unity/Assets/Scripts/SaveStore.cs:54-65`).
- [ ] **Video and the recording play.** A decision card's clip plays, and the 1948 recording plays; `Player.log` has
      `VIDEO playing` (`unity/Assets/Scripts/UI/CardVideo.cs:136`) and `RECORDING playing`
      (`unity/Assets/Scripts/UI/DeclarationRecording.cs:135`).

## Steam Deck (the Linux depot)

- [ ] **The Linux depot on a Deck.** With a native Linux depot the Deck runs the Linux build in the Steam Linux Runtime,
      not the Windows build under Proton (spec 4.4, "Steam Deck and controllers"; owner answer Q10, "No claim + one smoke
      test"). Start the game on a Deck, then play one turn with the touchscreen and the trackpads. No Deck claim is made
      on the store page.

## macOS (notarised)

- [ ] **The overlay opens on the notarised build.** The signed and notarised macOS player (#905) carries the two
      entitlements the overlay needs, `com.apple.security.cs.disable-library-validation` and
      `com.apple.security.cs.allow-dyld-environment-variables`, and no App Sandbox (Valve's Platforms page, read 1 Nov
      2026; spec 4.3). Install it from Steam, start it, and open the overlay with Shift+Tab.
