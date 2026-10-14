using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 slice before cutover (spec 2026-10-12 section 2.1): the real content with the loop switch on
    /// (eras.yaml systems.loop: gdd in every era), monthly turns (rules.yaml turn_lengths all month) and the slice's city data
    /// (cities.yaml, unit_types.yaml, rules.yaml mvp and the GDD command kinds' attention_cost), all placeholders, and the
    /// design assumption D-D5 (one military camp and one Hakhshara at the start). Tests, the MVP
    /// goldens and the dev flag load it; the real eras.yaml and rules.yaml keep the defaults until cutover. It edits YAML
    /// nodes, not text, so a comment or the key order in the real files cannot break it. Balance values the owner has not set
    /// may live here, and never under content/ (controller ruling 2, 12 October 2026). R-LOOP-003.</summary>
    public static class MvpSlice
    {
        static readonly IDeserializer Reader = new DeserializerBuilder().WithDuplicateKeyChecking().Build();
        static readonly ISerializer Writer = new SerializerBuilder().Build();

        /// <summary>The slice's city (D-A4: Tel Aviv). Design values, not historical figures: none is a claim about Tel Aviv
        /// in 1948. Counts are population units of mvp.people_per_unit people (D-A1).</summary>
        public const string Cities = @"# PLACEHOLDER: owner sets every value below (MvpSlice.Overlay; never under content/ until the owner sets them).
cities:
  - id: tel_aviv
    housing_capacity: 120   # PLACEHOLDER: owner sets
    job_slots: 90           # PLACEHOLDER: owner sets
    civilians: 100          # PLACEHOLDER: owner sets
    employed: 80            # PLACEHOLDER: owner sets
    # PLACEHOLDER: owner sets (D-D5: one of each exists at start; a design assumption, not a claim about 1948).
    # More are built by pioneers (Phase 4).
    facilities: [military_camp, hakhshara]
";

        /// <summary>The slice's population types (D-A2: civilians and refugees are counts per city; halutzim are movable
        /// units) and their training (GDD "Progression").</summary>
        public const string UnitTypes = @"types:
  - { id: civilian, kind: population }
  - { id: refugee, kind: population }
  - id: recruit                  # civilian to recruit, through basic training (GDD ""Progression"")
    kind: population             # a count per city until Phase 5
    facility: military_camp
    training_turns: 3            # PLACEHOLDER: owner sets
    training_cost: 0.5           # PLACEHOLDER: owner sets (IL millions per group)
  - id: halutz                   # civilian to Halutz, through pioneer training in a Hakhshara (GDD ""Progression"")
    kind: unit                   # a movable unit (D-A2)
    facility: hakhshara
    training_turns: 4            # PLACEHOLDER: owner sets
    training_cost: 0.5           # PLACEHOLDER: owner sets (IL millions per group)
";

        /// <summary>Wraps <paramref name="readFile"/> (the loader's file reader): eras.yaml and rules.yaml come back with the
        /// slice's switches and its rules, cities.yaml and unit_types.yaml with the slice's city when the content has none, and
        /// every other file unchanged.</summary>
        public static Func<string, string?> Overlay(Func<string, string?> readFile)
        {
            if (readFile == null) throw new ArgumentNullException(nameof(readFile));
            return rel =>
            {
                var text = readFile(rel);
                if (text == null && rel == "cities.yaml") return Cities;
                if (text == null && rel == "unit_types.yaml") return UnitTypes;
                if (text == null) return null;
                if (rel == "eras.yaml")
                {
                    var root = Reader.Deserialize<Dictionary<object, object>>(text);
                    if (root != null && root.TryGetValue("eras", out var list) && list is List<object> eras)
                        foreach (var item in eras.OfType<Dictionary<object, object>>())
                        {
                            var systems = item.TryGetValue("systems", out var x) && x is Dictionary<object, object> d ? d : new Dictionary<object, object>();
                            systems["loop"] = EraSystems.Gdd;
                            item["systems"] = systems;
                        }
                    return Writer.Serialize(root);
                }
                if (rel == "rules.yaml")
                {
                    var root = Reader.Deserialize<Dictionary<object, object>>(text) ?? new Dictionary<object, object>();
                    root["turn_lengths"] = new List<object> { "month", "month", "month", "month", "month" };
                    if (!root.ContainsKey("mvp"))
                        root["mvp"] = new Dictionary<object, object>
                        {
                            ["people_per_unit"] = 1000,            // PLACEHOLDER: owner sets (D-A1 scale)
                            ["income_per_job_per_year"] = 0.06,    // PLACEHOLDER: owner sets (IL millions per employed unit a year; D-C6)
                            ["cohesion_housing_weight"] = 0.5,     // PLACEHOLDER (D-C1 open)
                            ["cohesion_jobs_weight"] = 0.5,        // PLACEHOLDER (D-C1 open; the two weights sum to 1)
                            ["arrivals_city"] = "tel_aviv",        // Phase 1 has one city: every arrival lands there
                            ["build_yishuv_turns"] = 6,            // PLACEHOLDER: owner sets (D-A3: durations are the owner's)
                            ["build_yishuv_cost"] = 2,             // PLACEHOLDER: owner sets (IL millions, paid at the start)
                        };
                    if (root.TryGetValue("attention_cost", out var ac) && ac is Dictionary<object, object> costs)
                    {
                        if (!costs.ContainsKey("assign_workers")) costs["assign_workers"] = 0.5;         // PLACEHOLDER: owner sets
                        if (!costs.ContainsKey("train")) costs["train"] = 1;                             // PLACEHOLDER: owner sets
                        if (!costs.ContainsKey("cancel_training")) costs["cancel_training"] = 0.5;       // PLACEHOLDER: owner sets
                        // D-A6 (#456): "unit orders free". The plan's reading of a decided answer, not a placeholder (#378).
                        if (!costs.ContainsKey("pioneer_work")) costs["pioneer_work"] = 0;
                        if (!costs.ContainsKey("stop_work")) costs["stop_work"] = 0;
                    }
                    return Writer.Serialize(root);
                }
                return text;
            };
        }

        /// <summary>The content at <paramref name="contentRoot"/> (a directory laid out like content/) through <see cref="Overlay"/>,
        /// with the same directory checks as ContentLoader.Load(string).</summary>
        public static Content Load(string contentRoot) => ContentLoader.Load(contentRoot, Overlay);
    }
}
