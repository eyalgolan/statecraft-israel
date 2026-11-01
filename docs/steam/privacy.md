# Statecraft: Israel: privacy

The statement for the store page's support fields (#2156). The owner chose on 1 Nov 2026 to keep Unity's engine
reporting on and name it ("Name them", recorded in `docs/oss/DECISIONS.md`, "Steam").

## Statement

Statecraft: Israel does not collect anything about you.

- The game has no account and no sign-in of its own, no advertising, no in-game purchases and no online features. It is
  free.
- The share and report buttons open a web page in your browser, and only when you press them; the game itself sends
  nothing to those sites.
- It stores its own files only on your computer: the autosave and your settings (language, quality, interface scale).
  On Steam, if Steam Cloud is on, Steam copies the autosave between your computers; the settings stay on each computer.
- On Steam, the game tells Steam which achievements you have earned, and Steam shows its overlay. What Steam collects
  and keeps, including your account, your play time and your achievements, is Valve's, under Valve's own privacy policy
  (https://store.steampowered.com/privacy_agreement/), not this game's.
- The game is built with the Unity engine, and the engine's own analytics and diagnostics settings are left on in the
  game's project settings. Whatever the engine reports under them goes to Unity, not to this game's makers, and is
  covered by Unity's privacy policy (https://unity.com/legal/privacy-policy).

The game's source code is public in this repository, so anyone can check these statements.

## What is on, and what Unity's documentation says (read 1 Nov 2026)

- `submitAnalytics: 1` in `unity/ProjectSettings/ProjectSettings.asset:96`. Unity's 6000.3 Player settings page
  (https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-settings-reference/comp-manager-group/class-player-settings)
  does not describe it, and the scripting-reference page for `PlayerSettings.submitAnalytics` returned 404, so this page
  states only that it is on, not what it sends.
- `m_EngineDiagnosticsEnabled: 1` in `unity/ProjectSettings/UnityConnectSettings.asset:15`. Unity's "Diagnostics project
  settings reference" for 6000.3
  (https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-settings-reference/comp-manager-group/diagnostics-settings)
  says: "Diagnostic Data is set to Disabled if your project isn't connected to Unity Cloud. To collect diagnostic data,
  you must link your project to Unity Cloud via Project Settings." The project is not linked: `cloudEnabled: 0` and an
  empty `cloudProjectId` (`ProjectSettings.asset:933-938`), and `UnityConnectSettings.asset:7` has `m_Enabled: 0`.
- Unity's cloud analytics, crash reporting and performance reporting are off (`m_Enabled: 0`,
  `unity/ProjectSettings/UnityConnectSettings.asset:16-40`).
- The game's own code makes no network request on the desktop: it reads its StreamingAssets with `UnityWebRequest`,
  which on the desktop is a file path or a `file://` URI (`unity/Assets/Scripts/Boot/GameInputs.cs:147-152`). The only
  other `https://` addresses in `unity/Assets/Scripts` are the pages the share and report buttons open in the browser
  (`UI/ShareShot.cs:21-70`, `UI/ReportLinks.cs:18`, `UI/ExternalLink.cs:17`) and a check of credit links
  (`UI/EventImageMapping.cs:80`).

The statement makes no claim about what Unity collects beyond these documented facts.
