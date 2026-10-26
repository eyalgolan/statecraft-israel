# GDD 0.3 catalogue mapping

`content/gdd03-balance.yaml` records the design data for the separate `gdd-0.3` ruleset (#1687). It does not select that ruleset or change the live legacy/gdd-0.1 simulation. Existing content sets without this file still load; an authored file is validated and included in the content hash. #1708 consumes only the existing movement/defence subset under exact identity `gdd-0.3`: `military.move_infantry`, `military.move_armoured`, `military.move_road`, `military.move_rail`, `military.stack_limit`, `military.fort_bonus`, `military.fortify_turns`, `military.fortify_bonus`, the six land biomes' `movement` and `defence_percent` parts, and the `lake`/`sea` impassability expressions. Consumed values must have the specified integral shape and Agreed/Default basis; absent, Deferred or malformed values are rejected. This does not promote Default data to Agreed or adopt the remainder of the catalogue.

Artillery allowance and terrain costs remain Deferred, with no number introduced. Its movement consumer is blocked; its existing firing contract and shot accounting remain unchanged. Adding symbolic artillery catalogue entries requires a separately approved 0.3-only overlay/load design so older identities' raw content hashes remain unchanged; no shared catalogue edit is made here. Permanent forts derive from existing `kind: fort` location records after overrides (currently Latrun and Iraq Suwaydan; roster subject to Amir), without new state/save data. Legacy/gdd-0.1 use their original runtime paths and hash inputs.

The pinned source is [#1668's balance.md at 605277df](https://github.com/eyalgolan/statecraft-israel/blob/605277df/docs/gdd/balance.md). Every backticked key in its Key tables has a literal catalogue key. Project keys (`maabara` through `rail_segment`) and existing biome ids also keep their names. This gives 142 keys; a test fixes that inventory independently of the loader. Tables that share one row across several keys are split into individual entries. Multi-tier values remain ordered as in the source, with the order and units in the note.

Each entry has an **Agreed**, **Default** or **Deferred** basis and a provenance note. Numbers are exact `decimal` quantities, including integers. `expression` preserves symbolic formulas without evaluating them. Composite rows have separately labelled `parts`: a project's agreed cost does not promote its default upkeep. The existing combat draw count carries its separately labelled default draw band and four outcome bands, since the source gives that table no additional key names. Unavailable outcomes are omitted, as the part notes explain. These are design values, not historical measurements or gameplay defaults.

The source's retained approved baselines map to Agreed. Derived values preserve their dependency's basis: prepared fields and starting Food break-even are Agreed; starting housing capacity is Default because its 24 housing projects are Default. The starting free-civilian expression `34 - A` is Deferred because A is Deferred. Agreed zero costs in retained projects are explicitly recorded. Camp construction and upkeep remain Default; the source's city baseline versus standalone camp/centre application is separately labelled. The queue-loss amount is Agreed, its per-turn non-stacking cadence Default. Fortify duration is Agreed and its bonus Default.

`start.army` must be Deferred expression `A`, without any numeric value or parts. The loader refuses numeric substitutions, numeric expressions and relabelling it Default or Agreed. #1717 requires a separate owner decision before this contract can change; only then may the complete population partition and `34 - A >= 0` be validated with an actual integer. This task guesses no army size.

## Six owner rulings and existing keys

The coordinator's catalogue citation is **owner ruling, 25 Oct 2026, on #1668**. The public follow-up comments were posted on 26 October; they are [the three changes](https://github.com/eyalgolan/statecraft-israel/pull/1668#issuecomment-6403926601) and [the three retained rules](https://github.com/eyalgolan/statecraft-israel/pull/1668#issuecomment-6403936877).

| Owner ruling | Existing catalogue key | Treatment |
| --- | --- | --- |
| Units move at once | None for order timing | Movement allowances and costs remain their source basis; an instant order does not approve a particular allowance. No timing key invented. |
| Economic defeat exists | None for defeat thresholds | Rule retained; no numeric threshold invented or borrowed from the old ruleset. New thresholds remain Deferred work outside this catalogue. |
| Borrowing and debt exist | None for debt limits | Rule retained; no debt limit invented or borrowed from the old ruleset. New limits remain Deferred work outside this catalogue. |
| Losing a city or region is not a defeat | None | No key invented. Rule implementation tracked separately in #1711. |
| Attacks and artillery fire do not spend Arms | `military.combat_arms` | Agreed value 0 with the owner-ruling citation. Existing live-loop values are not edited here; #1706 handles the new rule implementation. |
| Attention removed | None | No Attention key invented. Rule implementation tracked separately in #1688. |

Numeric parameters the owner did not set are not made Agreed by these rulings. The catalogue contains no new debt, economic-defeat or timing numbers. Other GDD 0.3 systems and the eventual switch-over have their own issues.
