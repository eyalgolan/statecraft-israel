### Changed
- The start menu's theme plays from the top once, then loops between Afik's proposed loop points, 00:10 to 04:03,
  instead of looping the whole clip (Refs #2338). The loop points are data
  (`unity/Assets/Resources/Music/menu-music.yaml`); the OGG is unchanged. At each seam the next source is scheduled to
  start at the loop start on the same `dspTime` the other ends. Not yet listened to in a web build: in the OGG the cut
  lands on a step of about -45 dBFS (measured with ffmpeg), and #2338 stays open for that check.
