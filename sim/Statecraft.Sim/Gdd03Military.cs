using System;
using System.Linq;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>#1708's approved 0.3 consumers and #1706's unhooked catalogue foundation. Reads exact catalogue data; never falls back to old balance values.</summary>
    internal static class Gdd03Military
    {
        internal static bool Applies(Content c) => c.Ruleset == SaveGame.Gdd03Ruleset;
        static ContentException Invalid(string key, string reason) =>
            new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key, reason);

        static Gdd03DesignValue Entry(Content c, string key)
        {
            if (c.Gdd03Balance == null || !c.Gdd03Balance.Values.TryGetValue(key, out var value) || value == null)
                throw Invalid(key, "required by gdd-0.3 military runtime");
            if (value.Basis != "Agreed" && value.Basis != "Default") throw Invalid(key, "runtime consumer is blocked while Deferred");
            return value;
        }

        static int Number(Gdd03DesignValue value, string key, int count, int index, bool positive)
        {
            if (value.Values == null || value.Values.Count != count || value.Parts == null || value.Parts.Count != 0
                || !string.IsNullOrEmpty(value.Expression) || (value.Basis != "Agreed" && value.Basis != "Default"))
                throw Invalid(key, FormattableString.Invariant($"requires an Agreed/Default integer list of length {count}"));
            var n = value.Values[index];
            // Bound percent additions and movement/path arithmetic as well as the decimal-to-int conversion.
            if (n != decimal.Truncate(n) || n < (positive ? 1 : 0) || n > int.MaxValue - 100)
                throw Invalid(key, "requires an exact " + (positive ? "positive" : "nonnegative") + " integer below int.MaxValue - 99");
            return (int)n;
        }

        internal static int Scalar(Content c, string key, bool positive = true) => Number(Entry(c, key), key, 1, 0, positive);

        /// <summary>R-ENEMY-003, #2114: the off-script enemy attack threshold, combat.enemy_attack_ratio (a unitless ratio,
        /// 1.50 by the owner's O4 ruling) in Combat.Preview's percent; it must be a whole percent.</summary>
        internal static int EnemyAttackRatioPercent(Content c)
        {
            const string key = "combat.enemy_attack_ratio";
            var value = Entry(c, key);
            if (value.Values == null || value.Values.Count != 1 || value.Parts == null || value.Parts.Count != 0 || !string.IsNullOrEmpty(value.Expression))
                throw Invalid(key, "requires one exact ratio");
            var percent = value.Values[0] * 100;
            if (percent != decimal.Truncate(percent) || percent < 1 || percent > int.MaxValue)
                throw Invalid(key, "requires a positive ratio in whole percent");
            return (int)percent;
        }

        static Gdd03DesignValue Part(Content c, string biome, string part)
        {
            var row = Entry(c, biome);
            if (row.Values == null || row.Values.Count != 0 || !string.IsNullOrEmpty(row.Expression)
                || row.Parts == null || !row.Parts.TryGetValue(part, out var value) || value == null)
                throw Invalid(biome + ".parts." + part, "required biome part");
            return value;
        }

        internal static int Terrain(Content c, string biome, string role)
        {
            int index = role == "infantry" ? 0 : role == "armour" ? 1 : -1;
            if (index < 0) throw Invalid(biome + ".parts.movement", "no approved movement column for role " + role);
            return Number(Part(c, biome, "movement"), biome + ".parts.movement (" + role + ")", 2, index, true);
        }

        internal static int Defence(Content c, string biome) =>
            Number(Part(c, biome, "defence_percent"), biome + ".parts.defence_percent", 1, 0, false);

        /// <summary>#1706 catalogue foundation only: intentionally not called from Validate or Combat.</summary>
        internal static Gdd03CombatCatalogue ReadCombatCatalogue(Content c)
        {
            if (!Applies(c)) throw new NotSupportedException("Combat catalogue groundwork requires synthetic gdd-0.3 content.");
            if (c.Gdd03Balance == null || c.Gdd03Balance.Ruleset != SaveGame.Gdd03Ruleset || c.Gdd03Balance.Values == null)
                throw Invalid("combat.draw_count", "requires the separate gdd-0.3 catalogue");
            var sources = new Dictionary<string, Gdd03CombatSource>(StringComparer.Ordinal);
            // content/gdd03-balance.yaml:447-475; GDD 605277df docs/gdd/balance.md:172,180-185.
            // The parent is Agreed (retained one draw); its thresholds, band and rows remain Default.
            Gdd03DesignValue Required(string key, string basis, string? part = null)
            {
                var at = part == null ? key : key + ".parts." + part;
                var root = Entry(c, key);
                var value = root;
                if (part != null && (root.Parts == null || !root.Parts.TryGetValue(part, out value) || value == null))
                    throw Invalid(at, "required combat catalogue part");
                value.Validate("values." + at);
                if (value.Basis != basis) throw Invalid(at, "requires its catalogue basis " + basis);
                sources.Add(part == null ? key : key + "/" + part, new Gdd03CombatSource(value));
                return value;
            }
            decimal[] List(Gdd03DesignValue v, string at, int count, bool parent = false)
            {
                if (v.Values.Count != count || !string.IsNullOrEmpty(v.Expression) || (!parent && v.Parts.Count != 0))
                    throw Invalid(at, FormattableString.Invariant($"requires exactly {count} numeric values and no expression or nested parts"));
                return v.Values.ToArray();
            }
            decimal[] PartList(string part, int count) => List(Required("combat.draw_count", "Default", part),
                "combat.draw_count.parts." + part, count);
            int Integer(decimal n, string at, bool positive = false)
            {
                if (n != decimal.Truncate(n) || n < (positive ? 1 : 0) || n > int.MaxValue)
                    throw Invalid(at, "requires an exact bounded integer");
                return (int)n;
            }
            var parent = Required("combat.draw_count", "Agreed");
            var draws = Integer(List(parent, "combat.draw_count", 1, true)[0], "combat.draw_count", true);
            if (draws != 1) throw Invalid("combat.draw_count", "only the catalogued single draw is supported");
            var expectedParts = new[] { "ratio_thresholds", "draw_band", "below_0_5", "from_0_5_below_1", "from_1_below_2", "at_least_2" };
            if (parent.Parts.Count != expectedParts.Length || parent.Parts.Keys.Except(expectedParts, StringComparer.Ordinal).Any())
                throw Invalid("combat.draw_count.parts", "requires exactly the six catalogued parts");
            var thresholds = PartList("ratio_thresholds", 3);
            if (!thresholds.SequenceEqual(new[] { 0.5m, 1m, 2m }))
                throw Invalid("combat.draw_count.parts.ratio_thresholds", "requires the four catalogued ratio ranges");
            var band = PartList("draw_band", 2);
            if (band[0] != 0 || band[1] != 99) throw Invalid("combat.draw_count.parts.draw_band", "requires inclusive 0..99");
            var rows = new IReadOnlyList<Gdd03CombatBand>[4];
            for (var row = 0; row < rows.Length; row++)
            {
                var name = expectedParts[row + 2];
                var values = PartList(name, row == 0 || row == 3 ? 6 : 8);
                var result = new List<Gdd03CombatBand>();
                var next = 0;
                for (var pair = 0; pair < values.Length / 2; pair++)
                {
                    var at = "combat.draw_count.parts." + name;
                    var first = Integer(values[pair * 2], at); var last = Integer(values[pair * 2 + 1], at);
                    if (first != next || last < first || last > 99)
                        throw Invalid(at, "requires ordered, complete, non-overlapping inclusive 0..99 bands");
                    result.Add(new Gdd03CombatBand(first, last, (Gdd03CombatOutcome)(pair + (row == 3 ? 1 : 0))));
                    next = last + 1;
                }
                if (next != 100) throw Invalid("combat.draw_count.parts." + name, "bands must cover every draw through 99");
                rows[row] = result.AsReadOnly();
            }
            // content/gdd03-balance.yaml:484-495; GDD docs/gdd/balance.md:175-177 at 605277df.
            decimal Role(string key)
            {
                var n = List(Required(key, "Default"), key, 1)[0];
                if (n <= 0) throw Invalid(key, "requires a positive exact role factor");
                return n;
            }
            var armour = Role("combat.armoured_factor"); var artillery = Role("combat.artillery_factor");
            var range = Integer(List(Required("combat.artillery_range", "Default"), "combat.artillery_range", 1)[0], "combat.artillery_range", true);
            return new Gdd03CombatCatalogue(thresholds, rows, draws, armour, artillery, range, sources);
        }

        internal static void Validate(Content c)
        {
            if (!Applies(c)) return;
            foreach (var key in new[] { "military.move_infantry", "military.move_armoured", "military.move_road", "military.move_rail", "military.stack_limit" })
                Scalar(c, key);
            Scalar(c, "military.fort_bonus", false);
            EnemyAttackRatioPercent(c);
            Scalar(c, "military.fortify_bonus", false);
            if (Scalar(c, "military.fortify_turns") != 1) throw Invalid("military.fortify_turns", "only the existing one-turn settlement is implemented");
            foreach (var biome in new[] { "coastal_plain", "lowland", "valley", "hills", "mountains", "desert" })
            {
                Terrain(c, biome, "infantry"); Terrain(c, biome, "armour"); Defence(c, biome);
            }
            foreach (var water in new[] { "sea", "lake" })
                if (Entry(c, water).Expression != "impassable" || Entry(c, water).Values.Count != 0 || Entry(c, water).Parts.Count != 0)
                    throw Invalid(water, "must remain impassable");
            foreach (var t in c.UnitTypes.Where(t => t.Side != ""))
                if (t.Class != "infantry" && t.Class != "armour" && t.Class != Combat.ArtilleryClass)
                    throw new ContentException("unit_types.yaml", t.Id + ".class", "no gdd-0.3 movement mapping for role " + t.Class);
            foreach (var fort in c.MapLocations.Where(l => l.Kind == "fort"))
                if (!(c.HexAt(fort.Q, fort.R) is MapHex h) || !h.Land)
                    throw new ContentException(ContentLoader.MapLocationsFileName, fort.Id, "permanent fort must occupy effective land hex");
        }
    }
}
