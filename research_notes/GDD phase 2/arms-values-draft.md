# The national Arms pool: the four values and their reasoning (research note, 2026-10-18)

Status: the values are the owner's design values, approved on #781 on 18 October 2026 ("Approve as drafted",
https://github.com/eyalgolan/statecraft-israel/issues/781#issuecomment-6251995002). They are in `content/rules.yaml`
(`mvp:` and `attention_cost:`), each labelled `# DESIGN VALUE`, and the balance pass (#813) may tune them. The
historical cross-check further down is **an AI draft: a person must check it**. None of its sources is in `content/`,
and none carries a `checked:` date.

## The approved values

| # | Question (#781) | Approved value | Key |
|---|---|---|---|
| 1a | What one Arms is | The equipment package for one 1,000-person recruit group | (definition, R-ARMS-001) |
| 1b | Stock on 15 May 1948 | 12 Arms | `mvp.arms_start: 12` |
| 2 | Price | IL 1m per Arms; a short Treasury borrows the rest (R-MONEY-003) | `mvp.arms_price: 1` |
| 3 | Arms per recruit group | 1, taken when training starts; without one the training is refused with a reason, the state unchanged and the Attention refunded (Q-UNIT-3) | `mvp.arms_per_recruit_group: 1` |
| 4 | Attention for the buy order | 1 per order, whatever the quantity | `attention_cost.buy_arms: 1` |

## Reasoning

The owner gave the values, not a written rationale. What follows is how the values fit the rules already in the repo,
written by Claude when building the pool.

- **One Arms per recruit group.** A training group is one population unit, `people_per_unit` (1,000) people
  (`Training.cs`, R-TRAIN-001). Defining one Arms as that group's equipment makes training cost exactly 1 Arms, so the
  stock reads directly as "groups the state can still equip". Arms stays a whole number (spec §2.2).
- **12 on 15 May 1948.** The units already on the map carry their own weapons (Q-UNIT-4). The stock is the spare
  equipment for new recruits: 12 recruit groups, 12,000 soldiers, before the first purchase.
- **IL 1m per Arms.** Twice a recruit group's `training_cost` (IL 0.5m) and a twentieth of `treasury_start` (IL 20m).
  At this price buying Arms is a real Money decision: equipping a group costs IL 1.5m in all (Arms plus training).
  This answers the draft's own caveat that at its price "Money will rarely limit Arms in play".
- **1 Attention per order.** It matches `train`, `map_action`, `start_project` and `choose_event_option` (each 1). One
  order buys any whole number of Arms, so the Attention cost does not scale with the amount, and with 4 Attention a
  turn a purchase still competes with training and events.
- Not a number, but part of the rule: a purchase arrives at once, with no delivery delay. No plan text asks for a
  delay, and adding one would be a new rule.
- Cancelling a recruit cohort (R-TRAIN-002) returns its Arms to the pool, though not the Money (owner, #1047,
  18 October 2026: "Refund on cancel").

## Where the approved values differ from the AI draft

The AI draft posted on #781 (https://github.com/eyalgolan/statecraft-israel/issues/781#issuecomment-6251471128)
proposed a smaller unit. The owner approved different numbers:

| # | AI draft (18 October 2026) | Approved |
|---|---|---|
| 1a | One unit = the weapons and basic ammunition for 100 soldiers (a tenth of a group) | One Arms = the equipment for a 1,000-person group |
| 1b | 100 units (10 groups' equipment) | 12 Arms (12 groups) |
| 2 | IL 0.004m a unit (alternative 0.005), derived from 1948 plan prices | IL 1m an Arms, a design value |
| 3 | 10 units per group | 1 per group |
| 4 | 1 Attention per order | 1 Attention per order (unchanged) |

In the draft's unit, 12 Arms would be 120 units, a little above the draft's 100. The draft's price, scaled to a whole
group's kit, would be about IL 0.037m (10 x IL 0.0037m); the approved IL 1m is about 27 times that. The approved price
is therefore a balance value, not a historical price, which is how it is labelled.

The draft chose a tenth-of-a-group unit so that attacks (Q-COMBAT-4, Phase 5, #747) could spend Arms in small whole
amounts. With the approved unit, the Arms an attack spends is still open and is set by that PR.

## Historical cross-check (AI draft, a person must check)

Kept from the draft as an order-of-magnitude cross-check only. Claude read these sources on the web on 18 October
2026. No person has checked them.

1. Wikipedia, "Arms shipments from Czechoslovakia to Israel",
   https://en.wikipedia.org/wiki/Arms_shipments_from_Czechoslovakia_to_Israel. The 14 January 1948 contract was for
   200 MG 34, 4,500 rifles and 50,400,000 rounds. The *Nora* arrived on 2 April 1948 with 4,500 rifles and 200
   machine guns; the 28 April shipment brought 10,000 rifles and 1,415 machine guns. Total confirmed deliveries to
   October 1948: 34,500 rifles and 5,515 MG 34s. The page gives no prices. It is a secondary source; it cites Morris,
   *1948*.
   - Against the approved stock: the two April sea shipments alone (about 14,500 rifles) are about 14 groups' rifles,
     the same order as 12 Arms.
2. Ben-Gurion Archive, diary, 1 February 1948 (English translation),
   https://bengurionarchive.bgu.ac.il/en/node/15272. A procurement list with planned prices, not paid invoices:
   "10,000 rifles 480,000", "25 million bullets 2,057,000 ($ 82.3 per thousand)", "1,500 [heavy] machine [guns]s
   669,000", "40,000 shells 960,000", plus 40 75 mm guns at $400,000; total $4,566,000.
   - The draft's derivation: 100 rifles ($4,800) + 100,000 rounds ($8,230) + 4 heavy machine guns ($1,784) =
     $14,814 per 100 soldiers. The 4 machine guns (for "a few") are Claude's assumption. At about $4.03 to the
     Palestine pound (Claude's figure, not in the repo) that is about IL 0.0037m; at the repo's 1950-51 rate of
     IL 0.36 per US$ (`rules.yaml`) about IL 0.0053m.
3. Lapidot, "The Ten-Day Battle", https://www.daat.ac.il/DAAT/english/history/lapidot/34.htm. Jerusalem's weapons
   only: 1,011 rifles and 502 sub-machine guns on 15 May 1948, attributed to Izhak Levy, *Jerusalem in the War of
   Independence*, p. 473. City level only.
4. Wikipedia, "Tanks of the Israel Defense Forces", https://en.wikipedia.org/wiki/Tanks_of_the_Israel_Defense_Forces.
   States that by September 1947 the Haganah had 10,489 rifles, 702 light machine guns, 2,666 sub-machine guns and
   186 medium machine guns. The sentence has no direct citation on the page.

Not reached: a national Haganah or IDF weapons count dated 15 May 1948; any paid contract price for the Czech deals;
a sourced 1948 IL-to-US$ rate.

## What a person must check

1. Source 1's quantities and dates against Morris, *1948*, or another printed history.
2. Source 2's figures against the Hebrew diary original, and whether the plan prices are the prices paid.
3. Whether a national weapons count for 15 May 1948 exists (Gelber's work on arms procurement is the likely place),
   and whether 12 spare groups' equipment is not wildly off it.
4. The $14,814 sum (4 machine guns for "a few") and the $4.03 rate, if the balance pass (#813) wants a historical
   anchor for the price.
5. Any later `sources:` entry from this note needs a person's check first (CONTENT_POLICY §2).
