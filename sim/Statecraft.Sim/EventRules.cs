using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>Load-time checks for events, including the sensitive-content rule.</summary>
    static class EventRules
    {
        public static readonly HashSet<string> SensitiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "holocaust", "terror", "massacre", "war-casualties" };
        public static readonly HashSet<string> SpecialTargets = new HashSet<string> { "treasury", "fx_reserves" };
        /// <summary>Targets a sensitive event's options may not raise (CONTENT_POLICY section 4: no mechanic rewards violence against
        /// civilians). Owner decision of 15 October 2026 on Q-EFX-2, "Extend it": every resource, not money alone. Until the
        /// GDD resources exist as targets the legacy indicators stand for them: Money (treasury, fx_reserves), Food
        /// (food_supply), Social cohesion (morale) and International Support (intl_standing). Arms has no target yet.</summary>
        public static readonly HashSet<string> NoGainTargets = new HashSet<string> { "treasury", "fx_reserves", "food_supply", "morale", "intl_standing" };

        public static void Validate(Content c, HashSet<string> offices, HashSet<string> indicators)
        {
            var ids = new HashSet<string>();
            foreach (var e in c.Events)
            {
                string F(string field) => $"events[{e.Id}].{field}";
                if (!Offices.OfficeId.IsMatch(e.Id)) throw new ContentException(e.File, F("id"), ContentLoader.IdRule);
                if (!ids.Add(e.Id)) throw new ContentException(e.File, F("id"), "duplicate event id");
                if (e.Anchor != "hard" && e.Anchor != "soft") throw new ContentException(e.File, F("anchor"), $"unknown anchor '{e.Anchor}' (must be hard or soft)");
                ContentLoader.Status(e.File, F("status"), e.Status);
                ContentLoader.Review(e.File, $"events[{e.Id}]", e.ReviewedBy, e.ReviewedByGiven, e.ReviewedOn, e.ReviewedOnGiven);
                ContentLoader.Period(e.File, $"events[{e.Id}]", e.From, e.To);
                // Chapters design 9: an event may not span a boundary; it is split. An open window (no `to`) is allowed only in
                // the last era, since EventSystem keeps an open event eligible to the end of the game.
                var era = c.Eras.FirstOrDefault(x => x.From <= e.From && e.From < x.To);
                if (era != null && (e.To.HasValue ? e.To.Value > era.To : era.To < c.Eras.Max(x => x.To)))
                    throw new ContentException(e.File, F("to"), FormattableString.Invariant($"the window runs past the end of era '{era.Id}' ({era.To:yyyy-MM-dd}); an event may not span a boundary, split it"));
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException(e.File, F("office"), $"unknown office '{e.Office}'");
                if (c.Offices.Any(o => o.Id == e.Office && o.HistoricalOnly))
                    throw new ContentException(e.File, F("office"), "historical-only titles cannot be delegated events");
                CheckPlaceholders(c, e, F("title"), e.Title, offices);
                CheckPlaceholders(c, e, F("text"), e.Text, offices);
                // Chapters design 3.2: an event may only name model records that are active for its whole window.
                void Active(string field, string id)
                {
                    var ind = c.Indicators.FirstOrDefault(x => x.Id == id);
                    var pol = c.Policies.FirstOrDefault(x => x.Id == id);
                    var (rf, rt) = ind != null ? (ind.From, ind.To) : pol != null ? (pol.From, pol.To) : ((DateTime?)null, (DateTime?)null);
                    if (!Window.Covers(rf, rt, e.From, e.To)) throw new ContentException(e.File, field, $"'{id}' is not active for the whole window of this event");
                }
                foreach (var cond in e.Conditions)
                {
                    Active(F($"conditions.{cond.Indicator}"), cond.Indicator);
                    if (!indicators.Contains(cond.Indicator)) throw new ContentException(e.File, F($"conditions.{cond.Indicator}"), "unknown indicator");
                    // Indicators live in [0,1], so a bound outside it (or min above max) could never be met.
                    if (cond.Min.HasValue) { ContentLoader.Finite(e.File, F($"conditions.{cond.Indicator}.min"), cond.Min.Value); ContentLoader.Unit(e.File, F($"conditions.{cond.Indicator}.min"), cond.Min.Value); }
                    if (cond.Max.HasValue) { ContentLoader.Finite(e.File, F($"conditions.{cond.Indicator}.max"), cond.Max.Value); ContentLoader.Unit(e.File, F($"conditions.{cond.Indicator}.max"), cond.Max.Value); }
                    if (cond.Min > cond.Max) throw new ContentException(e.File, F($"conditions.{cond.Indicator}.min"), "must not exceed max");
                }
                if (e.Requires != null)
                {
                    if (e.Requires.Event == e.Id) throw new ContentException(e.File, F("requires.event"), "an event cannot require itself");
                    var required = c.Events.FirstOrDefault(x => x.Id == e.Requires.Event)
                        ?? throw new ContentException(e.File, F("requires.event"), $"unknown event '{e.Requires.Event}'");
                    if (required.Options.All(o => o.Id != e.Requires.Option))
                        throw new ContentException(e.File, F("requires.option"), $"event '{required.Id}' has no option '{e.Requires.Option}'");
                }
                if (e.Options.Count == 0) throw new ContentException(e.File, F("options"), "at least one option required");
                if (e.Options.Count(o => o.Historical) > 1) throw new ContentException(e.File, F("options"), "at most one option can be historical");
                if (e.Anchor == "hard" && !e.Options.Any(o => o.Historical)) throw new ContentException(e.File, F("options"), "hard anchor needs a historical option");
                bool sensitive = e.Tags.Any(SensitiveTags.Contains);
                var optionIds = new HashSet<string>();
                foreach (var o in e.Options)
                {
                    if (!Offices.OfficeId.IsMatch(o.Id)) throw new ContentException(e.File, F($"options[{o.Id}].id"), ContentLoader.IdRule);
                    if (!optionIds.Add(o.Id)) throw new ContentException(e.File, F($"options[{o.Id}].id"), "duplicate option id");
                    CheckPlaceholders(c, e, F($"options[{o.Id}].text"), o.Text, offices);
                    foreach (var kv in o.SetPolicy)
                    {
                        if (c.Policies.All(p => p.Id != kv.Key)) throw new ContentException(e.File, F($"options[{o.Id}].set_policy.{kv.Key}"), "unknown policy");
                        ContentLoader.Unit(e.File, F($"options[{o.Id}].set_policy.{kv.Key}"), kv.Value);
                        Active(F($"options[{o.Id}].set_policy.{kv.Key}"), kv.Key);
                    }
                    if (o.SetRung.HasValue && (o.SetRung < 1 || o.SetRung > 5)) throw new ContentException(e.File, F($"options[{o.Id}].set_rung"), "must be 1-5");
                    foreach (var fx in o.Effects)
                    {
                        if (!indicators.Contains(fx.Target) && !SpecialTargets.Contains(fx.Target))
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), "unknown target");
                        Active(F($"options[{o.Id}].effects.{fx.Target}"), fx.Target);
                        ContentLoader.Finite(e.File, F($"options[{o.Id}].effects.{fx.Target}.delta"), fx.Delta);
                        if (sensitive && NoGainTargets.Contains(fx.Target) && fx.Delta > 0)
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), $"sensitive events cannot reward {fx.Target}");
                    }
                }
            }
            CheckRequirementCycles(c);
            CheckObjectives(c);
        }

        /// <summary>Objectives and their cards name each other (R-OBJ-003): an event's `objective` names an objective whose
        /// `card` is that event, and an objective's card exists, names it back and has exactly one option (the card reports a
        /// result; there is nothing to choose).</summary>
        static void CheckObjectives(Content c)
        {
            const string file = ContentLoader.ObjectivesFileName;
            var seen = new HashSet<string>();
            foreach (var o in c.Objectives)
            {
                string F(string field) => $"objectives[{o.Id}].{field}";
                if (!Offices.OfficeId.IsMatch(o.Id)) throw new ContentException(file, F("id"), ContentLoader.IdRule);
                if (!seen.Add(o.Id)) throw new ContentException(file, F("id"), "duplicate id");
                ContentLoader.Named(file, F("name"), o.Name);
                if (o.CompletesWhen == null || o.CompletesWhen.SiteBuilt == "")
                    throw new ContentException(file, F("completes_when"), "required: { site_built: <site id> }");
                if (o.ReferenceDate == default) throw new ContentException(file, F("reference_date"), "required (the sourced date of the real event)");
                var card = c.Events.FirstOrDefault(e => e.Id == o.Card)
                    ?? throw new ContentException(file, F("card"), $"unknown event '{o.Card}'");
                if (card.Objective != o.Id)
                    throw new ContentException(card.File, $"events[{card.Id}].objective", $"must be '{o.Id}': the event is that objective's card");
                if (card.Options.Count != 1)
                    throw new ContentException(card.File, $"events[{card.Id}].options", "an objective's card has exactly one option");
            }
            foreach (var e in c.Events.Where(e => e.Objective != ""))
            {
                var o = c.Objectives.FirstOrDefault(x => x.Id == e.Objective)
                    ?? throw new ContentException(e.File, $"events[{e.Id}].objective", $"unknown objective '{e.Objective}'");
                if (o.Card != e.Id)
                    throw new ContentException(e.File, $"events[{e.Id}].objective", $"objective '{o.Id}' has another card, '{o.Card}'");
            }
        }

        /// <summary>Re-review N4: a cycle of `requires` (A needs B, B needs A, ...) could never fire. Called after every
        /// requirement's ids are known to exist; the error names the files around the cycle, starting from the first event
        /// (in load order) that lies on it.</summary>
        static void CheckRequirementCycles(Content c)
        {
            var byId = c.Events.ToDictionary(e => e.Id);
            foreach (var start in c.Events)
            {
                var path = new List<GameEvent> { start };
                var seen = new HashSet<string> { start.Id };
                for (var e = start; e.Requires != null; )
                {
                    e = byId[e.Requires.Event];
                    path.Add(e);
                    if (e.Id == start.Id)
                        throw new ContentException(start.File, $"events[{start.Id}].requires",
                            "requirements form a cycle: " + string.Join(" -> ", path.Select(x => x.File)));
                    if (!seen.Add(e.Id)) break;   // a cycle that does not pass through start; reported from its own member
                }
            }
        }

        static void CheckPlaceholders(Content c, GameEvent e, string field, string text, HashSet<string> offices)
        {
            if (Offices.Placeholder.Replace(text, "").IndexOfAny(new[] { '{', '}' }) >= 0)
                throw new ContentException(e.File, field, "stray '{' or '}' outside an {office_id} placeholder");
            foreach (Match m in Offices.Placeholder.Matches(text))
            {
                var office = m.Groups[1].Value;
                if (!offices.Contains(office)) throw new ContentException(e.File, field, $"unknown office placeholder '{m.Value}'");
                if (c.Offices.Any(o => o.Id == office && o.SeveralHolders))
                    throw new ContentException(e.File, field, $"office '{office}' can have several holders at once, so '{m.Value}' would not name one person");
                var gap = FirstUncovered(c, office, e.From, e.To);
                if (gap.HasValue)
                    throw new ContentException(e.File, field, FormattableString.Invariant($"office '{office}' has no holder on {gap.Value:yyyy-MM-dd}, inside the event window"));
            }
        }

        /// <summary>First date in [from, to) with nobody holding office, or null. Holders only change at term
        /// starts and ends and at a holder's death, so checking those dates (plus from) covers the whole window.</summary>
        static DateTime? FirstUncovered(Content c, string office, DateTime from, DateTime? to)
        {
            bool Inside(DateTime d) => d >= from && (to == null || d < to.Value);
            var terms = c.People.SelectMany(p => p.Offices).Where(t => t.Office == office).ToList();
            var points = new SortedSet<DateTime> { from };
            foreach (var t in terms)
            {
                if (Inside(t.From)) points.Add(t.From);
                if (t.To.HasValue && Inside(t.To.Value)) points.Add(t.To.Value);
            }
            foreach (var p in c.People.Where(p => p.Died.HasValue && Inside(p.Died.Value) && p.Offices.Any(t => t.Office == office)))
                points.Add(p.Died!.Value);
            // An open-ended window needs no extra check: a last finite term ends inside [from, ∞) and is caught above.
            foreach (var d in points)
                if (Offices.Holder(c, office, d) == null) return d;
            return null;
        }
    }
}
