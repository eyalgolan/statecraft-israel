### Fixed
- In the Unity Editor, the special buildings, the town building models and the whole-town landmarks no longer load,
  create or destroy models while a Scene or Game view camera is rendering (#2099): the change waits for the next
  update, as it already did in play mode and as the camp tents do since #2046. Editor only; players are unaffected.
