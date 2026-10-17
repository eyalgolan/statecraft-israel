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
        public static readonly HashSet<string> SpecialTargets = new HashSet<string> { "treasury", "fx_reserves", Pools.FoodTarget };
        /// <summary>Targets a sensitive event's options may not raise (CONTENT_POLICY section 4: no mechanic rewards violence against
        /// civilians). Owner decision of 15 October 2026 on Q-EFX-2, "Extend it": every resource, not money alone. Until the
        /// GDD resources exist as targets the legacy indicators stand for them: Money (treasury, fx_reserves), Food
        /// (food_supply), Social cohesion (morale) and International Support (intl_standing). Food also has its own target
        /// (`food`, R-EFX-001); Arms has no target yet and joins the rule with its pool (#781).</summary>
        public static readonly HashSet<string> NoGainTargets = new HashSet<string> { "treasury", "fx_reserves", Pools.FoodTarget, "food_supply", "morale", "intl_standing" };

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
                for (int i = 0; i < e.Requires.Count; i++)
                {
                    CheckRequirement(c, e, $"requires[{i}]", e.Requires[i]);
                    // Two entries on one event can never both hold, or one makes the other redundant: either way it is a slip.
                    if (e.Requires.Take(i).Any(r => r.Event == e.Requires[i].Event))
                        throw new ContentException(e.File, F($"requires[{i}].event"), $"event '{e.Requires[i].Event}' is already required; give it one entry");
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
                    if (o.Alternative != null) CheckAlternative(e, o, F($"options[{o.Id}].alternative"));
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
                        if (fx.Target == Pools.FoodTarget && Math.Abs(fx.Delta) > int.MaxValue)
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), "food delta out of range");
                        if (sensitive && NoGainTargets.Contains(fx.Target) && fx.Delta > 0)
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), $"sensitive events cannot reward {fx.Target}");
                    }
                }
            }
            CheckRequirementCycles(c);
            CheckObjectives(c);
        }

        /// <summary>The options' structured effects (D-H2): R-EFX-006 refuses a target that does not exist at load, so Resolve
        /// never skips one; R-EFX-005 refuses any gain from them on a sensitive event, as R-EFX-002 does for the resources.
        /// Called once the GDD files (cities, unit types, sites) are read, so after <see cref="Validate"/>.</summary>
        public static void ValidateStructured(Content c)
        {
            foreach (var e in c.Events)
                foreach (var o in e.Options)
                    CheckStructured(c, e, o, e.Tags.Any(SensitiveTags.Contains), field => $"events[{e.Id}].{field}");
        }

        static void CheckStructured(Content c, GameEvent e, EventOption o, bool sensitive, Func<string, string> F)
        {
            if (o.GrantUnits.Count + o.CityEffects.Count == 0) return;
            // They act on the GDD records (cities.yaml, unit_types.yaml, sites.yaml), which the legacy loop does not load.
            if (!c.IsGdd)
                throw new ContentException(e.File, F($"options[{o.Id}].{(o.GrantUnits.Count > 0 ? "grant_units" : "city_effects")}"), "needs the GDD loop (eras.yaml systems.loop: gdd)");
            for (int i = 0; i < o.GrantUnits.Count; i++)
            {
                var g = o.GrantUnits[i];
                string G(string field) => F(FormattableString.Invariant($"options[{o.Id}].grant_units[{i}]{field}"));
                if (sensitive) throw new ContentException(e.File, G(""), "sensitive events cannot reward units");
                var type = c.UnitTypes.FirstOrDefault(t => t.Id == g.Type)
                    ?? throw new ContentException(e.File, G(".type"), $"unknown unit type '{g.Type}'");
                if (type.Kind != "unit") throw new ContentException(e.File, G(".type"), $"'{g.Type}' is not a unit type (kind {type.Kind})");
                if (g.N < 1) throw new ContentException(e.File, G(".n"), "must be at least 1");
                if (g.At == "" && c.Cities.Count(x => x.Facilities.Contains(EventSystem.MilitaryCamp)) != 1)
                    throw new ContentException(e.File, G(".at"), "required: no single city has the military camp, so name a city");
                // R-EFX-006: a target must exist on the event's date. Every cities.yaml city is in the game from the first
                // turn; a site has no window and starts unbuilt, so it cannot be checked against a date yet.
                if (g.At != "" && c.Cities.All(x => x.Id != g.At))
                    throw new ContentException(e.File, G(".at"), c.Sites.Any(x => x.Id == g.At)
                        ? $"'{g.At}' is a site: sites cannot be grant targets until they carry a date (R-EFX-006)"
                        : $"unknown city '{g.At}'");
            }
            for (int i = 0; i < o.CityEffects.Count; i++)
            {
                var x = o.CityEffects[i];
                string X(string field) => F(FormattableString.Invariant($"options[{o.Id}].city_effects[{i}]{field}"));
                if (c.Cities.All(k => k.Id != x.City)) throw new ContentException(e.File, X(".city"), $"unknown city '{x.City}'");
                if (x.Field != EventSystem.HousingField && x.Field != EventSystem.JobsField)
                    throw new ContentException(e.File, X(".field"), $"unknown field '{x.Field}' (housing_capacity or job_slots)");
                if (x.Delta == 0) throw new ContentException(e.File, X(".delta"), "must not be 0");
                if (sensitive && x.Delta > 0) throw new ContentException(e.File, X(""), $"sensitive events cannot reward {x.Field}");
            }
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

        /// <summary>R-ALT-003: the shape of an option's `alternative` record.</summary>
        static void CheckAlternative(GameEvent e, EventOption o, string at)
        {
            var a = o.Alternative!;
            if (o.Historical) throw new ContentException(e.File, at, "the historical option is the record, not an alternative");
            if (a.Basis != OptionAlternative.Proposed && a.Basis != OptionAlternative.Debated && a.Basis != OptionAlternative.Design)
                throw new ContentException(e.File, at + ".basis", $"'{a.Basis}' must be proposed, debated or design");
            if (a.Basis == OptionAlternative.Design)
            {
                if (a.By != "" || a.Date.HasValue)
                    throw new ContentException(e.File, at + (a.By != "" ? ".by" : ".date"), "a design alternative names nobody and no date");
            }
            else
            {
                if (a.By.Trim() == "") throw new ContentException(e.File, at + ".by", "a proposed or debated alternative names who put it forward");
                if (a.By.IndexOfAny(new[] { '{', '}' }) >= 0) throw new ContentException(e.File, at + ".by", "words only, no placeholder: name the body or office");
                if (!a.Date.HasValue) throw new ContentException(e.File, at + ".date", "needs the date it was put forward");
                if (a.Sources.Count == 0) throw new ContentException(e.File, at + ".sources", "needs at least one source");
            }
            foreach (var src in a.Sources)
                if (!src.Url.StartsWith("https://", StringComparison.Ordinal) || src.Checked == default)
                    throw new ContentException(e.File, at + ".sources", "needs an https url and a checked date");
        }

        /// <summary>R-ALT-001 and R-ALT-002: a requirement names an existing other event and either one of its options or
        /// `diverged: true`, which needs that event to have a historical option.</summary>
        internal static void CheckRequirement(Content c, GameEvent e, string at, EventRequirement r)
        {
            string F(string field) => $"events[{e.Id}].{at}{field}";
            if (r.Event == e.Id) throw new ContentException(e.File, F(".event"), "an event cannot require itself");
            var required = c.Events.FirstOrDefault(x => x.Id == r.Event)
                ?? throw new ContentException(e.File, F(".event"), $"unknown event '{r.Event}'");
            if (r.Diverged && r.Option != "") throw new ContentException(e.File, F(""), "give an option or diverged: true, not both");
            if (!r.Diverged && r.Option == "") throw new ContentException(e.File, F(""), "needs an option or diverged: true");
            if (r.Diverged && !required.Options.Any(o => o.Historical))
                throw new ContentException(e.File, F(".diverged"), $"event '{required.Id}' has no historical option, so it cannot diverge");
            if (!r.Diverged && required.Options.All(o => o.Id != r.Option))
                throw new ContentException(e.File, F(".option"), $"event '{required.Id}' has no option '{r.Option}'");
        }

        /// <summary>Re-review N4: a cycle of `requires` (A needs B, B needs A, ...) could never fire. Called after every
        /// requirement's ids are known to exist. It walks each list in order; the error names the files around the first
        /// cycle found, starting from the first event (in load order) that lies on one.</summary>
        static void CheckRequirementCycles(Content c)
        {
            var byId = c.Events.ToDictionary(e => e.Id);
            foreach (var start in c.Events)
            {
                var cycle = CycleFrom(byId, start, start, new List<GameEvent> { start }, new HashSet<string> { start.Id });
                if (cycle != null)
                    throw new ContentException(start.File, $"events[{start.Id}].requires",
                        "requirements form a cycle: " + string.Join(" -> ", cycle.Select(x => x.File)));
            }
        }

        static List<GameEvent>? CycleFrom(Dictionary<string, GameEvent> byId, GameEvent start, GameEvent at, List<GameEvent> path, HashSet<string> seen)
        {
            foreach (var r in at.Requires)
            {
                var next = byId[r.Event];
                if (next.Id == start.Id) return new List<GameEvent>(path) { next };
                if (!seen.Add(next.Id)) continue;   // already walked from start; a cycle not through start is reported from its own member
                path.Add(next);
                var found = CycleFrom(byId, start, next, path, seen);
                if (found != null) return found;
                path.RemoveAt(path.Count - 1);
            }
            return null;
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
