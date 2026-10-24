#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.UI;
using Statecraft.Sim;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.EditorTools
{
    /// <summary>The exact English and Hebrew settlement-control strings the owner sensitivity-reviewed (R-GATE-002).
    /// The checked-in manifest remains pending until that human review happens.</summary>
    public sealed class SettlementControlStringReview
    {
        public sealed class Row
        {
            public string Key { get; set; } = "";
            public string En { get; set; } = "";
            public string He { get; set; } = "";
        }

        public string Status { get; set; } = "";
        public List<Row> Strings { get; set; } = new List<Row>();

        /// <summary>Parses a review manifest, rejecting malformed YAML and unknown fields with the file name.</summary>
        public static SettlementControlStringReview Parse(string yaml, string file)
        {
            try
            {
                return new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<SettlementControlStringReview>(yaml ?? "")
                    ?? throw new InvalidOperationException(file + ": empty settlement-control strings review");
            }
            catch (YamlDotNet.Core.YamlException e)
            {
                throw new InvalidOperationException(file + ": " + e.Message, e);
            }
        }

        /// <summary>Why the manifest cannot authorize a shareable build, or null. Missing data fails closed.</summary>
        public static string? Refusal(SettlementControlStringReview? review, UiText en, UiText he)
        {
            if (review == null)
                return "the settlement-control strings review is missing or unreadable";
            if (review.Status != "pending" && review.Status != ReleaseGate.ReviewedStatus)
                return "the settlement-control strings review has unknown status '" + review.Status + "'";
            if (review.Strings == null || review.Strings.Count == 0)
                return "the settlement-control strings review has no strings";
            if (review.Strings.Any(x => x == null || string.IsNullOrWhiteSpace(x.Key)))
                return "the settlement-control strings review has an empty key";
            var repeated = review.Strings.GroupBy(x => x.Key, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1);
            if (repeated != null)
                return "the settlement-control strings review has repeated key '" + repeated.Key + "'";
            foreach (var row in review.Strings)
            {
                if (!en.Has(row.Key)) return "strings.en.yaml is missing settlement-control key '" + row.Key + "'";
                if (!he.Has(row.Key)) return "strings.he.yaml is missing settlement-control key '" + row.Key + "'";
                if (en[row.Key] != row.En) return "settlement-control key '" + row.Key + "' does not match strings.en.yaml";
                if (he[row.Key] != row.He) return "settlement-control key '" + row.Key + "' does not match strings.he.yaml";
            }
            return review.Status == ReleaseGate.ReviewedStatus ? null
                : "a shareable build needs the owner's sensitivity review of the settlement-control strings"
                    + " (set status: sensitivity-reviewed in " + UiPaths.SettlementControlStringsReview + ")";
        }
    }
}
