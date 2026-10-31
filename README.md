# #2030 capture dialog: "one more decision waiting" reads singular, before and after

- Before: main `ae9019c4`. After: main `ae9019c4` merged with PR head `30210f7c` (local `--no-ff` merge, not pushed).
- Rendered in EditMode into a RenderTexture with the game UXML, panel settings and the real strings
  (`strings.en.yaml`, `strings.he.yaml` with the Hebrew class), as `CaptureDialogTests` does. Targets are test
  fixtures: Lydda, Ramle and Jaffa awaiting the capture choice, no event pending.
- `1more`: Ramle and Lydda waiting (dialog on Lydda, one more). `2more`: Ramle, Lydda and Jaffa (dialog on Jaffa,
  two more). Each at 1920x1080 and 390x844, cropped to the dialog.
- Not for merging.
