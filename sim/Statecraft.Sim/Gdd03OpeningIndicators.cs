namespace Statecraft.Sim
{
    /// <summary>#1715, R-NAT-001/003: Q17's opening points, not new indicator bases.
    /// Catalogue Default provenance is retained; Deferred or malformed values never fall back to the old game.</summary>
    internal static class Gdd03OpeningIndicators
    {
        internal static (double Cohesion, double Support) Read(Content c)
        {
            var maximum = Scalar(c, "indicator.display_max");
            if (maximum <= 0) throw Invalid("indicator.display_max", "requires a positive displayed maximum");
            var cohesion = Points(c, "start.cohesion", maximum);
            var support = Points(c, "start.support", maximum);
            // Divide exact catalogue decimals before the one conversion to the live state's double.
            return ((double)(cohesion / maximum), (double)(support / maximum));
        }

        static decimal Points(Content c, string key, decimal maximum)
        {
            var value = Scalar(c, key);
            if (value < 0 || value > maximum)
                throw Invalid(key, "requires opening points between zero and indicator.display_max");
            return value;
        }

        static decimal Scalar(Content c, string key)
        {
            var catalogue = c.Gdd03Balance;
            if (catalogue == null || catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "(file)", "required by gdd-0.3 opening indicators");
            if (!catalogue.Values.TryGetValue(key, out var row) || row == null
                || (row.Basis != "Agreed" && row.Basis != "Default") || row.Values == null || row.Values.Count != 1
                || row.Parts == null || row.Parts.Count != 0 || !string.IsNullOrEmpty(row.Expression))
                throw Invalid(key, "requires one Agreed or Default scalar; no Deferred or old-rules fallback");
            return row.Values[0];
        }

        static ContentException Invalid(string key, string reason) =>
            new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key, reason);
    }
}
