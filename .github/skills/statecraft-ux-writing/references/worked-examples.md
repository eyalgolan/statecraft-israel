# Worked UX copy proposals

These examples were checked against main at `e1251750` on 19 October 2026. They illustrate the workflow; they are proposed English wording, not an accepted glossary or native-reviewed Hebrew. Recheck the current implementation before reusing them.

## Changed save content

- **Player need:** understand why loading a save might produce different events.
- **Key:** `save.warning.content_changed` in [English strings](../../../../unity/Assets/UI/strings.en.yaml).
- **Current:** This save is from an earlier version of the game, so events may unfold a little differently.
- **Candidate:** The game has changed since this save was made. Events may unfold differently.
- **Evidence:** [GameSession](../../../../sim/Statecraft.Sim/GameSession.cs) compares the saved content hash with the current content hash. A mismatch establishes changed content, not which version is older.
- **Review:** owner wording choice and paired Hebrew review pending.

## A restored game differs

- **Player need:** understand the warning and decide whether to continue the restored game.
- **Key:** `save.warning.replay_differs` in [English strings](../../../../unity/Assets/UI/strings.en.yaml).
- **Current:** The game was restored, but not exactly as it was saved.
- **Candidate:** Some parts of your restored game differ from the save. Check your position before continuing.
- **Evidence:** [GameSession](../../../../sim/Statecraft.Sim/GameSession.cs) can return a changed-content replay with a different final hash. [GameStart](../../../../unity/Assets/Scripts/UI/GameStart.cs) resumes the loaded session and presents its warnings. The message must not claim the save is unusable or promise a repair button.
- **Review:** owner wording choice and paired Hebrew review pending; inspect the full warning in the load flow.

## A map action limit

- **Player need:** understand when another map action becomes possible.
- **Key:** `info.map_action_reason.turn_cap` in [English strings](../../../../unity/Assets/UI/strings.en.yaml).
- **Current:** No more than {0} map actions in one turn
- **Candidate:** The limit is {0} map actions per turn. Try again next turn.
- **Evidence:** [MapActions](../../../../sim/Statecraft.Sim/MapActions.cs) counts actions for the current turn date. A new date resets that count; other placement constraints can still apply. Preserve `{0}` and do not promise that the placement will succeed next turn.
- **Review:** owner wording choice and paired Hebrew review pending; inspect the message beside the unavailable action.

## Recording the next example

Use the same fields: player need, key or screen, current wording, candidate, evidence and review status. For new content, record the implemented entry point instead of inventing a before state. Keep accepted examples distinct from proposals, with the accepting PR as evidence.
