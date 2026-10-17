using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    public sealed class SourceRef { public string Url { get; set; } = ""; public string Checked { get; set; } = ""; }

    public sealed class Settlement
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "";
        /// <summary>The 1948 Hebrew name (#558), with its own sources; empty when the file gives none (the UI keeps the English).</summary>
        public string NameHe { get; set; } = ""; public List<SourceRef> NameHeSources { get; set; } = new List<SourceRef>();
        /// <summary>"agent-checked" while the Hebrew name waits for a native speaker's review (#564); empty once reviewed.</summary>
        public string NameHeReview { get => nameHeReview ?? ""; set => nameHeReview = value ?? ""; }
        /// <summary>True when the row has the name_he_review key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool NameHeReviewGiven => nameHeReview != null;
        string nameHeReview;
        public double Lon { get; set; } = double.NaN; public double Lat { get; set; } = double.NaN; // NaN = missing
        public int? Founded { get; set; }      // null for pre-modern cities (no verifiable founding year)
        public bool PreModern { get; set; }
        public List<SourceRef> Sources { get; set; } = new List<SourceRef>();
    }

    public sealed class SettlementFile { public List<Settlement> Settlements { get; set; } = new List<Settlement>(); }

    public static class SettlementPlacer
    {
        public const string DefaultFileName = "settlements_1948.yaml";

        // Marker colour and box edge (m) per type: placeholder art until the settlement art track.
        static readonly Dictionary<string, (Color c, float h)> Marker = new Dictionary<string, (Color, float)>
        {
            ["city"] = (new Color(0.93f, 0.92f, 0.88f), 1800f), ["town"] = (new Color(0.88f, 0.84f, 0.76f), 1100f),
            ["kibbutz"] = (new Color(0.80f, 0.35f, 0.25f), 600f), ["moshav"] = (new Color(0.78f, 0.45f, 0.30f), 600f),
            ["arab_town"] = (new Color(0.82f, 0.74f, 0.58f), 1100f), ["village"] = (new Color(0.76f, 0.68f, 0.52f), 600f),
        };

        /// <summary>The marker box's edge (m) for a settlement type; the UI's label sits on its top.</summary>
        public static float MarkerHeightM(string type) => Marker[type].h;
        /// <summary>The marker box's colour for a settlement type (SiteLayer draws a built site's box with the kibbutz's).</summary>
        public static Color MarkerColour(string type) => Marker[type].c;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");

        /// <summary>How many times <see cref="Parse"/> has run in this domain (a test aid: SettlementsOnceTests counts the
        /// parses of one boot, issue #321).</summary>
        internal static int Parses { get; private set; }

        /// <summary>Parses settlements_1948.yaml. Every malformed input throws InvalidDataException naming
        /// <paramref name="file"/>, and for content errors the settlement id and the field: YAML syntax, duplicate keys,
        /// unknown keys or bad scalars (with the id of the settlement they sit in); empty id or name; an id that is not
        /// lower snake_case; unknown type; duplicate id; missing or non-finite lon/lat (or outside
        /// <paramref name="bounds"/> when given); empty sources, or a source with an empty url or checked; a name_he without
        /// name_he_sources or the reverse (#558); founded null without pre_modern: true.</summary>
        public static List<Settlement> Parse(string yaml, (double lon0, double lon1, double lat0, double lat1)? bounds = null,
            string file = DefaultFileName)
        {
            Parses++;
            SettlementFile doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<SettlementFile>(yaml); }
            catch (YamlException e)
            {
                var id = SettlementIdAt(yaml, e.Start);
                throw new InvalidDataException(id == null ? $"{file}: {e.Message}" : $"{file}: '{id}' {e.Message}", e);
            }
            if (doc?.Settlements == null || doc.Settlements.Count == 0) throw new InvalidDataException($"{file}: no settlements");
            var ids = new HashSet<string>();
            foreach (var s in doc.Settlements)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.Id)) throw new InvalidDataException($"{file}: a settlement has an empty id");
                string at = $"{file}: '{s.Id}'";
                if (!IdRule.IsMatch(s.Id)) throw new InvalidDataException($"{at} id must be lower snake_case (^[a-z][a-z0-9_]*$)");
                if (!ids.Add(s.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (string.IsNullOrWhiteSpace(s.Name)) throw new InvalidDataException($"{at} has an empty name");
                if (s.Type == null || !Marker.ContainsKey(s.Type)) throw new InvalidDataException($"{at} has unknown type '{s.Type}'");
                CheckCoord(at, "lon", s.Lon, bounds?.lon0, bounds?.lon1);
                CheckCoord(at, "lat", s.Lat, bounds?.lat0, bounds?.lat1);
                if (s.Sources == null || s.Sources.Count == 0) throw new InvalidDataException($"{at} sources is empty");
                for (int i = 0; i < s.Sources.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(s.Sources[i]?.Url)) throw new InvalidDataException($"{at} sources[{i}].url is empty");
                    if (string.IsNullOrWhiteSpace(s.Sources[i].Checked)) throw new InvalidDataException($"{at} sources[{i}].checked is empty");
                }
                CheckHebrewName(at, s);
                if (s.Founded == null && !s.PreModern) throw new InvalidDataException($"{at} founded is null without pre_modern: true");
            }
            return doc.Settlements;
        }

        /// <summary>A Hebrew name needs its own sources (url and checked), and sources need the name (#558).</summary>
        static void CheckHebrewName(string at, Settlement s)
        {
            var sources = s.NameHeSources ?? new List<SourceRef>();
            bool named = !string.IsNullOrWhiteSpace(s.NameHe);
            if (named && sources.Count == 0) throw new InvalidDataException($"{at} name_he_sources is empty: a Hebrew name needs a source");
            if (!named && sources.Count > 0) throw new InvalidDataException($"{at} name_he is empty but name_he_sources is set");
            // Absent (a person has reviewed the name) or exactly agent-checked: a blank or null value is neither.
            if (s.NameHeReviewGiven && s.NameHeReview != "agent-checked")
                throw new InvalidDataException($"{at} name_he_review '{s.NameHeReview}' is not agent-checked (delete the line once a person has reviewed the name)");
            if (!named && s.NameHeReviewGiven) throw new InvalidDataException($"{at} name_he is empty but name_he_review is set");
            for (int i = 0; i < sources.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(sources[i]?.Url)) throw new InvalidDataException($"{at} name_he_sources[{i}].url is empty");
                if (string.IsNullOrWhiteSpace(sources[i].Checked)) throw new InvalidDataException($"{at} name_he_sources[{i}].checked is empty");
            }
        }

        /// <summary>The id of the settlement whose mapping contains <paramref name="at"/>, or null when the mark lies outside
        /// every settlement or the YAML cannot be walked that far. Walks parser events: root mapping (depth 1), the
        /// settlements sequence (2), one settlement mapping (3), whose keys and values alternate.</summary>
        static string SettlementIdAt(string yaml, Mark at)
        {
            var parser = new Parser(new StringReader(yaml));
            int depth = 0, node = 0; long start = -1; string key = null, id = null;
            try
            {
                while (parser.MoveNext())
                {
                    var ev = parser.Current;
                    if (ev is MappingStart || ev is SequenceStart)
                    {
                        if (depth == 3) node++;   // a collection value inside a settlement (sources)
                        if (++depth == 3) { start = ev.Start.Index; node = 0; id = null; }
                    }
                    else if (ev is MappingEnd || ev is SequenceEnd)
                    {
                        if (depth == 3 && start <= at.Index && at.Index <= ev.End.Index) return id;
                        depth--;
                    }
                    else if (ev is Scalar sc && depth == 3)
                    {
                        if (node % 2 == 0) key = sc.Value; else if (key == "id" && id == null) id = sc.Value;
                        node++;
                    }
                }
            }
            catch (YamlException) { }
            return null;
        }

        static void CheckCoord(string at, string field, double v, double? min, double? max)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) throw new InvalidDataException($"{at} {field} is missing or not finite ({v})");
            if (min.HasValue && (v < min.Value || v > max.Value))
                throw new InvalidDataException($"{at} {field} {v} outside the heightmap bounds {min.Value}..{max.Value}");
        }

        /// <summary>Placeholder markers (per-era art comes with the settlement art track): a box sized by type, under a returned
        /// "Settlements" root. The box is a plain MeshFilter + MeshRenderer (no collider) using the editor-made
        /// <paramref name="markerMaterial"/> (no runtime Shader.Find, M0 correction 1); the per-type colour goes through a
        /// MaterialPropertyBlock. Names are drawn by the UI (MapLabelsPresenter), not here.</summary>
        public static GameObject Place(List<Settlement> list, GeoFrame frame, Terrain terrain, Material markerMaterial)
        {
            var root = new GameObject("Settlements"); var parent = root.transform;
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var block = new MaterialPropertyBlock();
            foreach (var s in list)
            {
                var p = frame.ToWorld(s.Lon, s.Lat, 0); p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                var (c, h) = Marker[s.Type];
                var box = new GameObject(s.Id); box.transform.SetParent(parent);
                box.AddComponent<MeshFilter>().sharedMesh = cube;
                var r = box.AddComponent<MeshRenderer>(); r.sharedMaterial = markerMaterial;
                block.SetColor(BaseColor, c); r.SetPropertyBlock(block);
                box.transform.position = p + Vector3.up * h / 2; box.transform.localScale = new Vector3(h, h, h);
            }
            return root;
        }
    }
}
