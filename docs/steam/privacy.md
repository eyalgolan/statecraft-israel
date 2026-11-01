# Statecraft: Israel: privacy

**Draft for the owner** (#2156). The statement below is for the store page's support fields. One point is open and is
marked **[owner: confirm]**; until it is settled, the statement must not say that nothing at all leaves the computer.

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
- The game is built with the Unity engine. **[owner: confirm]** what the engine itself reports (see below), and say it
  here in one line.

The game's source code is public in this repository, so anyone can check these statements.

## The open point: Unity's own reporting

What the repository shows (read 1 Nov 2026):

- The game's own code makes no network request on the desktop: it reads its StreamingAssets with `UnityWebRequest`,
  which on the desktop is a file path or a `file://` URI (`unity/Assets/Scripts/Boot/GameInputs.cs:147-152`). The only
  other `https://` addresses in `unity/Assets/Scripts` are the pages the share and report buttons open in the browser
  (`UI/ShareShot.cs:21-70`, `UI/ReportLinks.cs:18`, `UI/ExternalLink.cs:17`) and a check of credit links
  (`UI/EventImageMapping.cs:80`).
- Unity's cloud services are off: analytics, crash reporting and performance reporting all have `m_Enabled: 0`
  (`unity/ProjectSettings/UnityConnectSettings.asset:24-40`).
- But two engine-level settings are on: `submitAnalytics: 1` (`unity/ProjectSettings/ProjectSettings.asset:96`) and
  `m_EngineDiagnosticsEnabled: 1` (`unity/ProjectSettings/UnityConnectSettings.asset:15`). They belong to the engine's own
  usage and diagnostics reporting to Unity. This page does not state what they send or whether a Unity Personal licence
  can turn them off: that has to be read from Unity's documentation for 6000.3 before the statement is final.

The owner decides between: turning them off in the Player settings (if the licence allows it) and saying the game sends
nothing; or keeping them and naming Unity's reporting, with a link to Unity's privacy policy, in the statement.
