### Changed

- Require the units named by an operation decision to be in one of that operation's regions before it can begin. A
  delegated operation whose units are elsewhere is postponed, keeps its option, and waits for its units for up to 6 turns
  (`mvp.operation_wait_turns`); it lapses earlier when a unit it needs is gone or its front's armistice is signed, and a
  lapse costs 0.01 Security (`mvp.operation_lapse_security`). While that armistice is undecided the operation waits for
  it. A late launch's unit orders end later by the time it waited, and one launched inside a truce the player accepted
  costs 0.10 International Support unless its own effects already do. Rhodes Talks, Sinai Withdrawal and Rafah follow a
  late Horev, and drop once Egypt signs.
  Operation cards' unit orders now also apply under gdd-0.3, and an objective, prisoner-exchange or Deport card can no
  longer give unit orders (the content loader refuses it). The escalations (Yoav's push to Hebron, Hiram's advance into
  Lebanon) and Ten Days' southern front give orders too; Givati gains a Bayt Jibrin order target.
  A card shown after its own window has closed reads its late text (`late_text`, with `late_text_he`); five cards
  have one, and the translators' page (`tools/translate.py`) lists it.
- The decision card lists each required unit once, shows an operation waiting for its front's armistice after that
  armistice and a waiting operation after the turn's other cards, takes a queued operation back when its unit is moved
  away, and moves the toast below a short photograph band. The turn report has a row for postponed and lapsed operations.
- The automated players no longer stage an operation's units onto an enemy-held place.
