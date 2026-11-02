### Changed
- Bringing back the three GDD 0.3 cards (#2229) moves no pinned golden: Hash1950, Hash1952, mvp-end, the smoke runs
  and every scenario golden stay the same, because the live game plays gdd-0.1 and never reads
  `content/gdd03-events.yaml`, and no golden is defined for gdd-0.3. Under gdd-0.3 the content hash changes, and a run
  through the restored cards ends in a different state. A gdd-0.3 save made before this still loads, with the "content
  changed" warning (and "replay differs" when its replay ends elsewhere); the shipped content plays gdd-0.1, so no
  released build writes gdd-0.3 saves yet, and the save format is unchanged. The Israel Bonds exclusion now gives the real blocker: the owner's Debt rulings need Debt as an
  event target and the dated dollar rate (#2224).
