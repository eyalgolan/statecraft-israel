# PR #1227 captures: Hebrew map region details, before and after

- Before: main `7fce3facc`. After: the PR head `a12fd9cc3` (PR #1227 merged with main `fdc829d25`, which has #1406's Gazette sub-district names).
- Native Unity EditMode renders of the shipped Game.uxml, styles and InfoPanelPresenter with the real map data, one
  selected hex per case, flat background, no map camera. Same date, hex, language and window size in each pair; each
  `.txt` gives them and the exact text.
- Fixture: Avl's `RegionHebrewVisualProof.cs` (avlco/statecraft-israel `aef45769`, `docs/review-1219/`), run unchanged.
- Cases: subdistrict (15 May 1948), auja (1 Mar 1949), jordan (8 May 1949), lebanon (1 Aug 1949), modus-vivendi
  (1 Mar 1950); EN and HE; 1920x1080 and 390x844.
- English: all 10 PNGs byte-identical before and after.
