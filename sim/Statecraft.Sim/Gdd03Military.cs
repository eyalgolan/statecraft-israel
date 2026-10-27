using System;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Only #1708's approved 0.3 consumers. Reads exact catalogue data; never falls back to old balance values.</summary>
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

        internal static void Validate(Content c)
        {
            if (!Applies(c)) return;
            foreach (var key in new[] { "military.move_infantry", "military.move_armoured", "military.move_road", "military.move_rail", "military.stack_limit" })
                Scalar(c, key);
            Scalar(c, "military.fort_bonus", false);
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
