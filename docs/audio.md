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
- With no file (or any failed download) the start menu is silent for the rest of the page: no error and no warning,
  one plain `MUSIC no theme` line in the log. A reload tries again.
- Each file needs its rows like any other asset: `ASSETS.yaml` (source, licence, SHA-256), `LICENSING.md` and the
  credit. Keep it under the repository's 10 MiB per-file limit (`scripts/oss/oss_policy.py`).
