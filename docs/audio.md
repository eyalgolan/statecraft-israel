# Audio

Where the game's sounds live and what plays them (#39).

| What | Where | Played by |
|---|---|---|
| Sound effects (turn end, refusal, event card) | `unity/Assets/Resources/Audio/` | `GameAudio`, at the Settings "Sound effects" volume |
| Ben-Gurion's 1948 recording | `unity/Assets/StreamingAssets/audio/` | `DeclarationRecording`, from its button on the start screen ([declaration-recording.md](declaration-recording.md)) |
| Music | `unity/Assets/StreamingAssets/audio/music/` | `MenuMusic`, on the start screen, at the Settings "Music" volume |

## Music files

- Music goes in `unity/Assets/StreamingAssets/audio/music/`, as Ogg Vorbis (`.ogg`) only (owner, 2 Nov 2026).
  StreamingAssets is not imported or re-encoded by Unity, so the file ships byte for byte. It is not part of the first
  download: the whole file is downloaded when the theme is about to play (on the web, after the page's first click, tap
  or key), then decoded in full. It is not streamed.
- Memory: a decoded clip is uncompressed. On the web the browser decodes it to 32-bit float samples, which is about
  0.35 MB per second of stereo 44.1 kHz audio, for example about 88 MB for a 4 min 10 s theme. The clip is released
  when the menu's fade ends.
- The start menu's theme is the file `MenuMusic.File` names. A renamed file is renamed there too. Once a theme is
  committed, `MenuMusicTests.ThePlayerPicksTheThemeInStreamingAssetsMusic` fails while the two differ.
- The theme plays from 0:00 once, then loops between its loop points (#2338): Afik's proposed 00:10 to 04:03, kept as
  data in `unity/Assets/Resources/Music/menu-music.yaml` (`file`, `loop_start_s`, `loop_end_s`), not in the OGG, which
  stays as delivered. Resources, not the music folder: that folder holds OGG files only, and the game reads Resources
  at start with no second download. At each seam, two `AudioSource`s hand over on the audio clock: one ends and the
  other starts at the loop start at the same `dspTime`, so there is no gap. A missing or invalid region (no file,
  another file's name, an end before the start or past the clip's end) loops the whole clip, with one plain
  `MUSIC no loop region` line in the log. `MenuMusicTests.TheThemeLoopsBetweenAfiksLoopPoints` checks the region
  against the committed file.
- With no file (or any failed download) the start menu is silent for the rest of the page: no error and no warning,
  one plain `MUSIC no theme` line in the log. A reload tries again.
- Each file needs its rows like any other asset: `ASSETS.yaml` (source, licence, SHA-256), `LICENSING.md` and the
  credit. Keep it under the repository's 10 MiB per-file limit (`scripts/oss/oss_policy.py`).
