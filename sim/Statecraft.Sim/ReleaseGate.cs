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

        /// <summary>R-ALT-011 (Q-ALT-9): every consequence card and every event with variants that no person has signed off
        /// (no reviewed_by), in id order. A shareable build refuses while it is not empty.</summary>
        public static IReadOnlyList<string> AlternateUnsigned(Content c) =>
            c.Events.Where(e => (e.Kind == EventKinds.Consequence || e.Variants.Count > 0) && e.ReviewedBy == "")
                .Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
}
