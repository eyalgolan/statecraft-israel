- Windows open and close with motion (#1106, play-test of v0.1.2): every side-menu screen and the decision card scale
  up and fade in over 0.5 s when they open and scale down over 0.3 s when they close. USS transitions only, switched by
  ScreenNav.Show; a closing window is out of reach while it shrinks and leaves the layout when the shrink ends, also when
  it is closed while it still grows. Play mode only: an EditMode panel does not tick, so the layout tests still see
  windows at full size.
