# Ben-Gurion's 1948 recording on the start screen

The start screen can play David Ben-Gurion reading the sentence that proclaims the state, from the recording of the
ceremony of 14 May 1948. The owner asked for it; the rights basis and its open questions are decision D-R in
[`docs/oss/DECISIONS.md`](oss/DECISIONS.md).

## The source

- **Item:** Israel State Archives, ISA-Collections-Recordings-00113hj, «העתק מן המקור של הקלטת ההכרזה על הקמת מדינת
  ישראל, כולל טקס החתימה» (a copy from the original recording of the declaration, with the signing ceremony).
  - The archive's own description, read on 2026-10-13 from the description of its YouTube upload below (saved by
    yt-dlp in the download's `.info.json`), says it was «הוקלט ע"י ראלף הלינגר בעל אולפן רדיו דוקטור» (recorded by Ralph
    Hellinger, owner of the Radio Doctor studio). The same description gives the catalogue link
    http://www.archives.gov.il/archives/#/Archive/0b07170680034d89/File/0b07170681096567.
  - The English Wikipedia article "Recording of the Israeli Declaration of Independence" (revision 1306487371)
    describes it as the archive's CD of the Kol Yisrael radio broadcast, "related to Hans and Ralf Hellinger from the
    'Radio Doctor' company". That is a different recording from the Tslil disc recording.
- **The copy used:** the archive's own upload on its YouTube channel, "ארכיון המדינה IsraelArchives" (channel
  `UCuZpTg6vY1QHBVZ95V5Z-lA`).
  - The video is https://www.youtube.com/watch?v=K5Q6Mrv4fGw, «הכרזת המדינה», uploaded 2023-05-22, 1444 s long.
  - Downloaded on 2026-10-13 with yt-dlp 2026.08.19 as YouTube audio format 251: Opus, 48 kHz stereo, in WebM.
  - The download is 22,845,078 bytes, sha256 `0d464f4ad06181364fe5bed86baeabb741429965c83694456be0db1261878cb8`.
  - The archive's catalogue page could not be read by script (the server answers 403), so the channel's copy is the
    archive's own publication that could be fetched and hashed.
- **Why not the Wikimedia Commons copy.** Commons has a different copy of the same broadcast, a 44.1 kHz mono Vorbis
  file 841.97 s long, re-encoded by an uploader.
  - Aligning the two by their waveforms puts the archive's 468.01 s at Commons 393.0 s. The Commons copy starts about
    75 s later.
  - The archive's own upload is one step closer to the archive, and the terms of use (below) are the archive's.

## The excerpt

- **The cut:** 468.01 to 477.30 s of the download, made by stream copy, so no audio is decoded or re-encoded:

  ```
  ffmpeg -i isa_00113hj_K5Q6Mrv4fGw.webm -ss 468.01 -to 477.30 -vn -map 0:a:0 -c:a copy -map_metadata -1 \
    -avoid_negative_ts make_zero -fflags +bitexact -flags:a +bitexact declaration.ogg
  ```

  - The cut is Ogg Opus, 147,033 bytes, sha256 `1e3d01c89b674cffad0658b6d4f87c6503debb37af0757be4c16b25f668b29a2`.
  - Its 465 Opus packets were compared one by one (`ffprobe -show_packets -show_data_hash sha256`). They are
    byte-identical, consecutive packets of the download, covering its 468.014 to 477.294 s.
- **The decode:** Unity's Web player could not load the Ogg Opus cut ("fetch: no clip" in Chrome), so it is decoded
  to 16-bit PCM WAV, with no other processing:

  ```
  ffmpeg -i declaration.ogg -map 0:a:0 -c:a pcm_s16le -map_metadata -1 -fflags +bitexact -flags:a +bitexact \
    declaration-1948-isa-00113hj.wav
  ```

  - Both commands were run with ffmpeg 9.0.1 on macOS (arm64). Two runs of each give identical files.
- **What ships:** `unity/Assets/StreamingAssets/audio/declaration-1948-isa-00113hj.wav`, 48 kHz stereo 16-bit PCM,
  9.29 s, 1,784,396 bytes, sha256 `24e7bc48f5d0134bbe28f0f81b3668235398e3a4bfd83d54aa8650aeae2da370`.
  - The hash is pinned in `DeclarationRecording.Sha256` and checked by a test, which proves the committed file has
    not changed. Re-running the two commands on the same download with the same ffmpeg gives the same bytes.
  - There is no normalising, no denoising, no fade and no re-encoding to a lossy format.
- **Listen-check:** an offline speech-to-text pass (faster-whisper "small", Hebrew, word timestamps) heard three
  phrases:
  - 0.00-2.00 s «אנו מחרזים בזאת». The model misspelt «מכריזים».
  - 2.60-6.40 s «על הקמת מדינה יהודית בארץ ישראל».
  - 6.92-8.76 s «היא מדינת ישראל».

  So the excerpt holds exactly «אנו מכריזים בזאת על הקמת מדינה יהודית בארץ־ישראל, היא מדינת ישראל», with no other
  word. The speaker was not identified by machine.

## On the start screen

- **A button.** "Play the recording" sits under the source line. Beside it is the credit "Recording: Ralph Hellinger,
  Israel State Archives", which names the creator and the archive as the terms ask.
- **Pressing it:**
  - While the file loads, the button reads "Loading the recording…". Pressing it again cancels.
  - When the audio is actually heard, the button becomes "Stop the recording" and the captions show.
  - The press ends with the message "The recording could not be played." when:
    - the fetch fails;
    - no data arrives for 8 s (a slow link that keeps receiving is never cut off);
    - the file has not arrived after 60 s in all;
    - it is not decoded within 5 s of arriving;
    - or the audio has not started 3 s after it was asked to play.
  - The end of the clip is when it was playing and now is not, so a slow first start never cancels itself.
  - Stopping releases the clip.
- **When it plays:**
  - It never plays by itself, and never on Continue or New Game.
  - It stops when the start screen closes.
  - It plays over the still painting only.
- **Captions.** The sentence shows in a dark strip above the panel, as the declaration's own texts in
  `docs/sources/declaration-1948`. The leading ellipsis marks a sentence quoted from its middle.
  - In Hebrew, in Frank Ruhl Libre, right to left: «…אנו מכריזים בזאת על הקמת מדינה יהודית בארץ־ישראל, היא מדינת
    ישראל.»
  - In English: "…HEREBY DECLARE THE ESTABLISHMENT OF A JEWISH STATE IN ERETZ-ISRAEL, TO BE KNOWN AS THE STATE OF
    ISRAEL."
  - `StartScreenTests.Quoted` checks both against the source files.
- **Automated runs** skip the start screen (`StartScreenPresenter.Skipped`). Without it the recording is never built,
  and the stylesheet keeps the button's row hidden.
- **Boot:** the 1.8 MB file is not in the build's data. It is fetched only when asked for.
