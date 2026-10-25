# Statecraft Israel — GDD v0.3 documentation bundle

Recreated 25 October 2026 from the recorded design decisions and edits in the conversation. Workspace maintenance removed the temporary files behind the original download links. The final archive was not found among the saved copies, so this is a reconstruction rather than a byte-for-byte recovery; wording has been consolidated while retaining the final decisions and document ownership.

Amir designated this consolidated design **v0.3 on 25 October 2026**. Its mechanics and numerical values remain those of the reconstructed draft; historical source snapshots and branch names retain their original versions. The published repository GDD remains separately labelled v0.2.

## Contents

The ZIP contains all 41 revised repository files, preserving their paths, plus a patch, a file/hash manifest and these notes. This is a replacement-file overlay, not a complete repository. Separate GDD and location copies are supplied for convenient reading.

- GDD: scope, authority, terrain/tile/hex types marked TBD and the single deferred-decision register.
- Locations: must-have/optional places, the military/reception camp roster, aliases and deployment-anchor classification.
- Balance: sole numerical catalogue, with explicit decisions distinguished from delegated initial defaults.
- Feature specs: population/reception/training, regions/housing/work, resources, combat, capture/governance, sites/corridors, objectives, calendar/resolution, events/end and map editor.
- Existing phase-plan paths: implementation handoffs pointing to immutable full originals, without active duplicate rule tables.
- Rule-ID index and related guides: current ownership pointers, with every existing tested ID preserved.

## Final decisions preserved

The reconstructed catalogue retains the final interview corrections, including a single trainee per teacher; Food rather than Money output from farming; the latest workshop cost tiers; actual builder work; actual-work farmer/pioneer progression; reusable Arms without attack spending; mandatory refugee reception; regional management and evacuation; the smooth housing-cost curve; housing-only upkeep deterioration; and removal of Attention and automatic loans/refill. The starting army count remains deferred as A, not a previously proposed guess.

Initial roles reconcile to the agreed total population, including teachers and the army rather than adding them. Initial staffed farm production equals consumption, with the agreed Food reserve. New arrivals do not cause automatic production scaling.

## Original base and historical conflicts

The source snapshot is main at e213d960188608f7edb590c8cb364f13e794b623, matching the original delivery. The location/editor source is 36443ec81e9f22d0edf793996fd07002a1d40936. Topic-branch overlap notes below describe its inspected 17 October state at c9cb11990acfc413a853ae6d365af01ac0d56516; they are not a new audit of today's branch.

| Historical conflict | Resolution retained |
| --- | --- |
| Approximately 48 turns versus the later owner calendar ruling | GDD uses 44 monthly turns from 15 May 1948 to 15 January 1952. |
| Hadera/Tiberias moved back to optional in uploads | Both remain must-have, preserving existing supported places. |
| Four-site implementation versus later six-site approval | Required founding membership includes all six approved sites; exact reference-date verification stays gated. |
| Camp names in unit art appearing to imply more training bases | The location spec separates deployment context from playable camp facilities. |
| Old anonymous city counts, unlimited facilities and housing-only refugee conversion | Per-unit identity, finite reception/training and regional work are authoritative. |
| Old Attention, borrowing, Food refill and attack Arms use | Superseded by the recorded final decisions. |
| Fixed city timers and repeated balance tables | Worker-driven projects and one catalogue. |
| Any-city automatic defeat and extra cash-shortage penalties | Region loss allows continuation; only housing deteriorates from unpaid upkeep. |
| Repeated rules in old phase plans | Canonical feature links plus immutable originals preserve history without competing active definitions. |

No historical source, licence or sensitivity sign-off has been upgraded. Reference check dates are inherited from the original records. Region/capital authoring, reception availability/starting-centre host, founding-date evidence, final terminal defeat, minister mechanics, terrain taxonomy and other explicitly deferred principles remain visible in the GDD.

## Validation of the reconstruction

- GDD feature linter passed.
- Repository Markdown file-link checker passed; 74 local heading links checked separately.
- Rule-ID checker passed; all 43 existing definitions retained once.
- 49 agreed catalogue rows checked and all 142 catalogue keys unique.
- Starting population, Food and housing arithmetic reconciled.
- Whitespace/conflict checks passed.
- Patch applied successfully to a clean temporary index at the original base, reproducing all 41 changed paths.
- ZIP contents and every file hash verified after creation.

No new simulation/Unity implementation or gameplay-test pass is claimed. No repository changes were published by this recovery.

## Applying the files

Extract the ZIP and copy the repository paths onto a reviewed checkout, or apply the supplied patch at the recorded base. Review differences before applying to a repository updated since that snapshot. Keep unrelated work. The manifest, patch and notes are delivery aids, not gameplay content.
