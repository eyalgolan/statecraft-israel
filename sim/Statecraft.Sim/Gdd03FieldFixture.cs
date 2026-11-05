using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One authored starting field (Q36): a regions.yaml `field_hexes` entry of its listing region.</summary>
    internal sealed class Gdd03Field
    {
        internal string Id { get; }
        internal string Region { get; }
        internal int Q { get; }
        internal int R { get; }
        internal Gdd03Field(string region, int q, int r)
        {
            // Invariant culture: under he-IL a negative q would otherwise carry a direction mark (#2402).
            Id = FormattableString.Invariant($"field/{q},{r}"); Region = region; Q = q; R = r;
        }
    }

    /// <summary>A month's Food quote per field, in field id order. Nothing moves; farming makes no Money.</summary>
    internal sealed class Gdd03FieldYield
    {
        internal IReadOnlyDictionary<string, int> Fields { get; }
        internal int Food { get; }
        internal int Money => 0; // GDD 605277df docs/gdd/cities.md:13: farming creates no Money.
        internal Gdd03FieldYield(SortedDictionary<string, int> fields)
        {
            Fields = new ReadOnlyDictionary<string, int>(fields);
            Food = fields.Values.Aggregate(0, (sum, food) => checked(sum + food));
        }
    }

    /// <summary>R-FIELD-002 under exact gdd-0.3, synthetic fixtures only (R-POP-004): no command, loader or turn hookup;
    /// <see cref="Settle"/> adds a month's yield to the Food stock (#2471). Owner, 3 November 2026, #1694: "One farmer per field (Recommended)"; the option read: "Each
    /// field takes one farmer; an unworked field produces nothing." Each authored field (Q36) is one Farm slot of
    /// `field.slots` (1) in the shared <see cref="Gdd03AssignmentFixture"/>, which stays the single writer and whose
    /// ledger refuses a farmer living outside the field's region (Q27). A lost field keeps its farmer and yields again
    /// once retaken (<see cref="Settle"/>); preparation (#1713), XP and Food demand are not here.</summary>
    internal sealed class Gdd03FieldFixture
    {
        readonly GameState state;
        readonly Gdd03AssignmentFixture work;
        readonly Gdd03FieldRates rates;
        readonly SortedDictionary<string, Gdd03Field> fields;
        readonly HashSet<int> settledMonths = new HashSet<int>(); // R-CITY-010: one settlement per month, per fixture

        internal static IReadOnlyList<Gdd03Field> Fields(Content content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (!content.UsesPopulationLedger) throw new InvalidOperationException("Field slots require gdd-0.3.");
            var byId = new SortedDictionary<string, Gdd03Field>(StringComparer.Ordinal);
            foreach (var region in content.PlayerRegions.Regions.Values)
                foreach (var (q, r) in region.FieldHexes)
                {
                    var field = new Gdd03Field(region.Id, q, r);
                    byId.Add(field.Id, field); // a hex is one field
                }
            return Array.AsReadOnly(byId.Values.ToArray());
        }

        internal static IEnumerable<PopulationTarget> Targets(Content content) =>
            Fields(content).Select(f => new PopulationTarget(f.Id, PopulationAssignmentKind.Work, f.Region));

        /// <summary>The ruling as slots: `field.slots`, which <see cref="Gdd03FieldRates.FromCatalogue"/> refuses unless 1.</summary>
        internal static IEnumerable<Gdd03FixtureSlot> Slots(Content content)
        {
            var fields = Fields(content);
            var slots = Gdd03FieldRates.FromCatalogue(content.Gdd03Balance
                ?? throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.field.slots", "required under gdd-0.3")).SlotsPerField;
            return fields.Select(f => new Gdd03FixtureSlot(f.Id, Gdd03FixtureWork.Farm, slots, "field")).ToArray();
        }

        /// <summary><paramref name="otherSlots"/> are the caller's other Work slots: one inventory covers every Work target.</summary>
        internal Gdd03FieldFixture(Content content, GameState state, IEnumerable<Gdd03FixtureSlot> otherSlots)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (otherSlots == null) throw new ArgumentNullException(nameof(otherSlots));
            fields = new SortedDictionary<string, Gdd03Field>(Fields(content).ToDictionary(f => f.Id, StringComparer.Ordinal), StringComparer.Ordinal);
            rates = Gdd03FieldRates.FromCatalogue(content.Gdd03Balance!);
            this.state = state;
            work = new Gdd03AssignmentFixture(content, state, Slots(content).Concat(otherSlots));
        }

        /// <summary>Puts a farmer on a field. Every refusal comes before any change; the shared fixture then refuses a
        /// person who is not a free civilian, a full field ("destination is full": one farmer per field) and, at the
        /// ledger, a farmer living in another region (Q27).</summary>
        internal Gdd03FixtureAssignmentResult Staff(int id, string field, bool israeliHeld)
        {
            if (field == null || !fields.ContainsKey(field)) throw new ArgumentException("Unknown field.");
            // Q37 (docs/GDD.md:373): "Only an Israeli-held hex can be farmed". The sim has no hex control yet, so the
            // caller states it.
            if (!israeliHeld) throw new ArgumentException("Only an Israeli-held field can be farmed (Q37).");
            // R-FIELD-002: only a farmer holds a field. Owner ruling, 4 November 2026, #1694 (Q22), asked "May only a
            // unit with the Farmer profession staff a field?", answered "yes":
            // https://github.com/eyalgolan/statecraft-israel/issues/1694#issuecomment-6623124984
            if (state.PopulationLedger.Get(id).Profession != CivilianProfession.Farmer)
                throw new ArgumentException("Only a farmer can work a field.");
            return work.Assign(id, field);
        }

        /// <summary>Takes a farmer off their field; a person on any other Work slot is refused unchanged.</summary>
        internal Gdd03FixtureAssignmentResult Release(int id)
        {
            var person = state.PopulationLedger.Get(id);
            if (person.Assignment.Kind != PopulationAssignmentKind.Work || person.Assignment.Target == null
                || !fields.ContainsKey(person.Assignment.Target))
                throw new ArgumentException("This person is not on a field.");
            return work.Assign(id, null);
        }

        /// <summary>27M-2: a field yields for the month when the month's snapshot has a farmer working it (#1716), and Q37:
        /// while it is still Israeli-held. The yield is `farmer.output` at the snapshot tier (R-VET-002) times
        /// `field_food_per_turn` (docs/GDD.md:385, #2112); an unworked field yields 0. A quote: no Food, XP or state change.</summary>
        internal Gdd03FieldYield Yield(MvpRules rules, Gdd03SyntheticMonthContext month, IReadOnlyCollection<string> israeliHeldNow)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (month == null) throw new ArgumentNullException(nameof(month));
            if (israeliHeldNow == null) throw new ArgumentNullException(nameof(israeliHeldNow));
            var working = new HashSet<int>(month.WorkEligibleIds);
            var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var field in fields.Keys)
            {
                var farmers = month.OpeningPeople.Where(p => working.Contains(p.Id) && p.Assignment.Target == field).ToArray();
                if (farmers.Length > 1) throw new ArgumentException("A field's snapshot holds more than one farmer.");
                var farmer = farmers.SingleOrDefault();
                result.Add(field, farmer != null && farmer.Profession == CivilianProfession.Farmer && israeliHeldNow.Contains(field)
                    ? rates.FoodPerTurn(farmer.Tier, rules) : 0);
            }
            return new Gdd03FieldYield(result);
        }

        /// <summary>R-FIELD-002 and R-CITY-010 under gdd-0.3 (#2471): adds the month's <see cref="Yield"/> to the national
        /// Food stock, each field's once, in ordinal field-id order, with one cause per field that yields. Control is the
        /// caller's <paramref name="israeliHeldNow"/> at settlement, besides the snapshot's freeze: a field lost by either
        /// adds nothing, and its farmer, tier and assignment stay as they are (D6: "its people stay"), so the next snapshot
        /// after retaking yields again with no new assignment. A month already settled by this fixture is refused unchanged
        /// (<see cref="ArgumentException"/>), so the stock cannot be credited twice for one month. The sum is checked before the stock changes.</summary>
        internal Gdd03FieldYield Settle(MvpRules rules, Gdd03SyntheticMonthContext month, IReadOnlyCollection<string> israeliHeldNow, List<Cause> why)
        {
            if (why == null) throw new ArgumentNullException(nameof(why));
            var y = Yield(rules, month, israeliHeldNow);
            if (settledMonths.Contains(month.MonthOrdinal)) throw new ArgumentException("This month is already settled.");
            var food = checked(state.Food + y.Food);
            foreach (var field in y.Fields.Where(x => x.Value > 0).OrderBy(x => x.Key, StringComparer.Ordinal)) // field-id order, stated here so it does not rest on the yield's dictionary type
                why.Add(new Cause("economy", "food", field.Value,
                    FormattableString.Invariant($"{field.Key}: its farmer yielded {field.Value} units of Food"), key: CauseKeys.FieldFood, args: new[] { field.Key }));
            state.Food = food;
            settledMonths.Add(month.MonthOrdinal);
            return y;
        }
    }
}
