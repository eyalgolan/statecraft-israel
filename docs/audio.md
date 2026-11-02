# Audio

Where the game's sounds live and what plays them (#39).

| What | Where | Played by |
|---|---|---|
| Sound effects (turn end, refusal, event card) | `unity/Assets/Resources/Audio/` | `GameAudio`, at the Settings "Sound effects" volume |
| Ben-Gurion's 1948 recording | `unity/Assets/StreamingAssets/audio/` | `DeclarationRecording`, from its button on the start screen ([declaration-recording.md](declaration-recording.md)) |
| Music | `unity/Assets/StreamingAssets/audio/music/` | `MenuMusic`, on the start screen, at the Settings "Music" volume |

## Music files

- Music goes in `unity/Assets/StreamingAssets/audio/music/`, as Ogg Vorbis (`.ogg`) only (owner, 2 Nov 2026).
  StreamingAssets is not imported or re-encoded by Unity, so the file ships byte for byte and is fetched only when it
  plays: it adds nothing to the first download.
- The start menu's theme is the file `MenuMusic.File` names. A renamed file is renamed there too;
  `MenuMusicTests.ThePlayerPicksTheThemeInStreamingAssetsMusic` fails while the two differ.
- With no file the start menu is silent: no error and no warning, one plain `MUSIC no theme` line in the log.
- Each file needs its rows like any other asset: `ASSETS.yaml` (source, licence, SHA-256), `LICENSING.md` and the
  credit. Keep it under the repository's 10 MiB per-file limit (`scripts/oss/oss_policy.py`).
