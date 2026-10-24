using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Statecraft.Sim
{
    /// <summary>Hebrew from a <see cref="TranslationSet"/> on the sim's typed records (#1618): the same *He properties the
    /// loader fills from the inline fields today, so the code that reads them does not change. Step 1 uses it in tests
    /// only; the loader keeps reading the inline Hebrew until a later step of #1618 switches it.</summary>
    public static class HebrewOverlay
    {
        sealed class Binding
        {
            public string File = "", List = "", Field = "";
            public Func<Content, IEnumerable<object>> Records = c => Enumerable.Empty<object>();
            public PropertyInfo Value = null!, Id = null!;
            public PropertyInfo? Review;
        }

        static Binding Bind<T>(string file, string list, string field, Func<Content, IEnumerable<T>> records, string value, string? review)
            where T : class => new Binding
        {
            File = file, List = list, Field = field, Records = c => records(c).Cast<object>(),
            Value = typeof(T).GetProperty(value) ?? throw new InvalidOperationException(typeof(T).Name + "." + value),
            Review = review == null ? null : typeof(T).GetProperty(review) ?? throw new InvalidOperationException(typeof(T).Name + "." + review),
            Id = typeof(T).GetProperty("Id") ?? throw new InvalidOperationException(typeof(T).Name + ".Id"),
        };

        // One per content field the sim holds in Hebrew. RealContent's HebrewOverlayTests checks, by reflection, that no
        // *He property of a content type is missing here.
        static readonly Binding[] Bindings =
        {
            Bind("offices.yaml", "offices", "title", c => c.Offices, nameof(Office.TitleHe), nameof(Office.TitleHeReview)),
            Bind("offices.yaml", "offices", "title_f", c => c.Offices, nameof(Office.TitleHeF), nameof(Office.TitleHeFReview)),
            Bind("parties.yaml", "parties", "name", c => c.Parties, nameof(Party.NameHe), nameof(Party.NameHeReview)),
            Bind("people.yaml", "people", "name", c => c.People, nameof(Person.NameHe), nameof(Person.NameHeReview)),
            Bind("legislatures.yaml", "legislatures", "body", c => c.Legislatures, nameof(Legislature.BodyHe), nameof(Legislature.BodyHeReview)),
            Bind("governments.yaml", "governments", "name", c => c.Governments, nameof(Government.NameHe), nameof(Government.NameHeReview)),
            Bind("map/locations_1948.yaml", "locations", "name", c => c.MapLocations, nameof(MapLocation.NameHe), nameof(MapLocation.NameHeReview)),
            Bind("projects.yaml", "projects", "name", c => c.ProjectKinds, nameof(ProjectKind.NameHe), nameof(ProjectKind.NameHeReview)),
            Bind("sites.yaml", "sites", "name", c => c.Sites, nameof(Site.NameHe), null),
            Bind("corridors.yaml", "corridors", "name", c => c.Corridors, nameof(CorridorDef.NameHe), null),
            Bind("objectives.yaml", "objectives", "name", c => c.Objectives, nameof(Objective.NameHe), nameof(Objective.NameHeReview)),
        };

        /// <summary>Content files whose Hebrew Unity reads itself (SettlementPlacer, Localities, UnitData): the sim has no
        /// typed record for them, so their keys are checked against the YAML (<see cref="Translations.Resolve"/>) only.</summary>
        public static readonly IReadOnlyList<string> UnityReads = new[]
        {
            "map/localities_1948.yaml", "map/settlements_1948.yaml", "map/units_1948.yaml",
        };

        /// <summary>The (type, property) pairs the overlay fills, events included.</summary>
        public static IEnumerable<(Type Type, string Property)> Covered =>
            Bindings.Select(b => (b.Value.DeclaringType!, b.Value.Name))
                .Concat(new[] { (typeof(GameEvent), nameof(GameEvent.TitleHe)), (typeof(GameEvent), nameof(GameEvent.TextHe)),
                    (typeof(EventOption), nameof(EventOption.TextHe)) });

        /// <summary>The Hebrew the typed records hold, as a translation set (no UI strings: those are not content).</summary>
        public static TranslationSet Snapshot(Content c)
        {
            var set = new TranslationSet { Lang = Translations.Hebrew };
            foreach (var e in c.Events)
                if (e.TitleHe != "")
                    set.Events.Add(new EventTranslation
                    {
                        Id = e.Id, Title = e.TitleHe, Text = e.TextHe, Review = e.TextHeReview,
                        Options = e.Options.Where(o => o.TextHe != "").Select(o => new KeyValuePair<string, string>(o.Id, o.TextHe)).ToList(),
                    });
            foreach (var b in Bindings)
                foreach (var r in b.Records(c))
                {
                    var text = (string)b.Value.GetValue(r)!;
                    if (text == "") continue;
                    set.Content.Add(new ContentTranslation
                    {
                        Key = new ContentKey(b.File, new[] { b.List, (string)b.Id.GetValue(r)! }, b.Field).ToString(),
                        Text = text, Review = b.Review == null ? "" : (string)b.Review.GetValue(r)!,
                    });
                }
            return set;
        }

        /// <summary>Empties every Hebrew field the overlay fills (a test starts from content with no Hebrew).</summary>
        public static void Clear(Content c)
        {
            foreach (var e in c.Events)
            {
                e.TitleHe = ""; e.TextHe = ""; e.TextHeReview = "";
                foreach (var o in e.Options) o.TextHe = "";
            }
            foreach (var b in Bindings)
                foreach (var r in b.Records(c))
                {
                    b.Value.SetValue(r, "");
                    b.Review?.SetValue(r, "");
                }
        }

        /// <summary>Puts a Hebrew set on the typed records. Throws on a key that names no record, an option the event does
        /// not have, or a review flag on a field that has none; keys of <see cref="UnityReads"/> files are left to Unity.</summary>
        public static void Apply(Content c, TranslationSet set)
        {
            if (set.Lang != Translations.Hebrew) throw new ArgumentException("the sim holds Hebrew only", nameof(set));
            const string file = "translations/he/";
            foreach (var t in set.Events)
            {
                var e = c.Events.FirstOrDefault(x => x.Id == t.Id)
                    ?? throw new ContentException(file + Translations.EventsFile, t.Id, "no event has this id");
                e.TitleHe = t.Title; e.TextHe = t.Text;
                if (t.Review != "") e.TextHeReview = t.Review;
                foreach (var o in t.Options)
                    (e.Options.FirstOrDefault(x => x.Id == o.Key)
                        ?? throw new ContentException(file + Translations.EventsFile, $"{t.Id}.options.{o.Key}", "the event has no such option")).TextHe = o.Value;
            }
            foreach (var t in set.Content)
            {
                var key = ContentKey.TryParse(t.Key) ?? throw new ContentException(file + Translations.ContentFile, t.Key, "not a content key");
                if (UnityReads.Contains(key.File)) continue;
                var b = Bindings.FirstOrDefault(x => x.File == key.File && x.Field == key.Field && key.Path.Count == 2 && key.Path[0] == x.List)
                    ?? throw new ContentException(file + Translations.ContentFile, t.Key, "the sim holds no Hebrew for this field");
                var r = b.Records(c).FirstOrDefault(x => (string)b.Id.GetValue(x)! == key.Path[1])
                    ?? throw new ContentException(file + Translations.ContentFile, t.Key, $"no record '{key.Path[1]}'");
                b.Value.SetValue(r, t.Text);
                if (t.Review != "")
                    (b.Review ?? throw new ContentException(file + Translations.ContentFile, t.Key + ".review", "this field has no review flag")).SetValue(r, t.Review);
            }
        }
    }
}
