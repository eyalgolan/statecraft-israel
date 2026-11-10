using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The release gate for sensitive content (spec 5.5; M3 final review I4). An event with a sensitive tag
    /// (holocaust, terror, massacre, war-casualties, displacement) needs the owner's sensitivity review, recorded by moving its YAML
    /// <c>status</c> to <c>sensitivity-reviewed</c>, before any build is shared. Build.Web -shareable refuses to build while
    /// <see cref="SensitiveUnreviewed"/> is not empty. Setting that status is an OWNER step (docs/m3-release.md).</summary>
    public static class ReleaseGate
    {
        public const string ReviewedStatus = "sensitivity-reviewed";

        /// <summary>The ids of every event carrying a sensitive tag, in id order.</summary>
        public static IReadOnlyList<string> SensitiveEvents(Content c) =>
            c.Events.Where(e => e.Tags.Any(EventRules.SensitiveTags.Contains)).Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>The sensitive events whose status is not yet sensitivity-reviewed, in id order.</summary>
        public static IReadOnlyList<string> SensitiveUnreviewed(Content c) =>
            c.Events.Where(e => e.Tags.Any(EventRules.SensitiveTags.Contains) && e.Status != ReviewedStatus)
                .Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>R-ALT-011 (Q-ALT-9): every consequence card, every event with variants and (R-WAR-008, #1709; owner,
        /// 9 November 2026: "Separate early text (Recommended)") every armistice card with an early text that no person has
        /// signed off (no reviewed_by), in id order. A shareable build refuses while it is not empty.</summary>
        public static IReadOnlyList<string> AlternateUnsigned(Content c) =>
            c.Events.Where(e => (e.Kind == EventKinds.Consequence || e.Variants.Count > 0 || e.EarlyText != "") && e.ReviewedBy == "")
                .Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>R-UNIT-006, Q-START-3 (owner, 15 October 2026; #743, owner, 20 October 2026): the units_1948.yaml rows a
        /// new game starts a unit from whose status is not yet fact-checked (by row id), and (#2722, owner, option (c), 5 November
        /// 2026) each field of a fact-checked row whose sources were checked after the row's checked_on, naming the row and the
        /// field, in id order. Once the sim reads a row it is game state, so a shareable build refuses while this is not empty.</summary>
        public static IReadOnlyList<string> StartingForcesUnchecked(Content c) =>
            c.StartingForces.OrderBy(f => f.Row, StringComparer.Ordinal).SelectMany(f => f.Status != "fact-checked" ? new[] { f.Row }
                : f.SourcesChecked.Where(x => string.CompareOrdinal(x.Checked, f.CheckedOn) > 0)
                    .Select(x => $"{f.Row}: {x.Field}, sources checked {x.Checked}, row checked {f.CheckedOn}")).ToList();

        /// <summary>R-CTRL-001, Q-TGT-3 (b) (owner, 18 October 2026; #783): the settlement_targets.yaml rows whose status is not
        /// yet sensitivity-reviewed, in id order. A shareable build refuses while this is not empty (#1274).</summary>
        public static IReadOnlyList<string> SettlementTargetsUnchecked(Content c) =>
            c.SettlementTargets.Where(t => t.Status != ReviewedStatus).Select(t => t.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
}
