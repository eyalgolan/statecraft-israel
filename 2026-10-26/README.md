# #1135 before/after, 26 Oct 2026

Before: main `3536e3ec`. After: the PR at `662f6bf2`. Each `compare-<lang>-<window>-<scale>-<state>.png` puts the
same state side by side; red boxes mark the areas whose pixels differ (24 px grid). `INDEX.txt` lists each still as
CHANGED or same.

- Windows: 1920x1080, 1280x720, and phones held upright 390x844 and 360x800.
- Scales: 100, 125 and 150 (the Settings choices; the panel's reference resolution is 1920x1080 / scale).
- States: `card` (a decision card open, Lydda selected), `map` (the card hidden, Lydda's info panel), `cabinet` (the
  Cabinet open).
- Render: an EditMode render of the real Game.uxml on the real PanelSettings, HUD, card, Cabinet, info panel and
  minimap presenters, opening of the historical game; no 3D map (a flat colour stands in for it).

At 100% on 1920x1080 and 1280x720 the card and map stills are pixel-identical to main; the English Cabinet stills
differ by a few pixels in the title's last letters (the screen's opening transition), not the layout.
