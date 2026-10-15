using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>The JSON Schemas in content/schema/ (#265). They give editors (yaml-language-server) a red line under a wrong
/// field name or a missing <c>sources:</c>. They are written by hand, so these tests keep them honest: every real file
/// must pass, the field names must equal the loader types' properties, and the schemas must reject the mistakes they exist for.
/// The validator is small and covers only the keywords the schemas use. A schema that uses any other keyword fails the
/// tests, so no rule can be silently ignored.</summary>
public class SchemaTests
{
    static readonly string Root = RealContentTests.ContentRoot();

    static JsonElement LoadSchema(string name) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "schema", name))).RootElement;

    // ---- the validator -------------------------------------------------------------------------------------------

    static readonly HashSet<string> Supported = new HashSet<string>
    {
        "$schema", "$ref", "title", "description", "definitions", "type", "properties", "additionalProperties", "required",
        "items", "minItems", "minLength", "pattern", "minimum", "maximum", "enum",
    };

    static void Check(JsonElement schema, JsonElement root, JsonNode? value, string path, List<string> errors)
    {
        foreach (var k in schema.EnumerateObject())
            if (!Supported.Contains(k.Name)) throw new InvalidOperationException($"schema keyword '{k.Name}' at {path} is not supported by this test's validator");

        if (schema.TryGetProperty("$ref", out var r))
        {
            var target = root;
            foreach (var part in r.GetString()!.Substring(2).Split('/')) target = target.GetProperty(part);
            Check(target, root, value, path, errors);
            return;
        }
        if (schema.TryGetProperty("enum", out var en) && !en.EnumerateArray().Any(e => value is JsonValue v && v.TryGetValue<string>(out var s) && s == e.GetString()))
            errors.Add($"{path}: must be one of {string.Join(", ", en.EnumerateArray().Select(e => e.GetString()))}");
        if (schema.TryGetProperty("type", out var type) && !TypeOk(type.GetString()!, value)) { errors.Add($"{path}: must be {type.GetString()}"); return; }

        if (value is JsonValue val)
        {
            if (val.TryGetValue<string>(out var str))
            {
                if (schema.TryGetProperty("minLength", out var ml) && str.Length < ml.GetInt32()) errors.Add($"{path}: must not be empty");
                if (schema.TryGetProperty("pattern", out var pat) && !Regex.IsMatch(str, pat.GetString()!)) errors.Add($"{path}: '{str}' does not match {pat.GetString()}");
            }
            else if (val.TryGetValue<double>(out var num) || (val.TryGetValue<long>(out var whole) && (num = whole) == whole))
            {
                if (schema.TryGetProperty("minimum", out var mn) && num < mn.GetDouble()) errors.Add($"{path}: must be at least {mn.GetDouble()}");
                if (schema.TryGetProperty("maximum", out var mx) && num > mx.GetDouble()) errors.Add($"{path}: must be at most {mx.GetDouble()}");
            }
        }
        else if (value is JsonArray arr)
        {
            if (schema.TryGetProperty("minItems", out var mi) && arr.Count < mi.GetInt32()) errors.Add($"{path}: needs at least {mi.GetInt32()} entries");
            if (schema.TryGetProperty("items", out var items))
                for (int i = 0; i < arr.Count; i++) Check(items, root, arr[i], $"{path}[{i}]", errors);
        }
        else if (value is JsonObject obj)
        {
            if (schema.TryGetProperty("required", out var req))
                foreach (var name in req.EnumerateArray().Select(x => x.GetString()!).Where(n => !obj.ContainsKey(n)))
                    errors.Add($"{path}: missing '{name}'");
            var props = schema.TryGetProperty("properties", out var p) ? p : default;
            schema.TryGetProperty("additionalProperties", out var extra);
            foreach (var kv in obj)
            {
                var sub = path + "." + kv.Key;
                if (props.ValueKind == JsonValueKind.Object && props.TryGetProperty(kv.Key, out var propSchema)) Check(propSchema, root, kv.Value, sub, errors);
                else if (extra.ValueKind == JsonValueKind.Object) Check(extra, root, kv.Value, sub, errors);
                else if (extra.ValueKind == JsonValueKind.False) errors.Add($"{sub}: unknown field");
            }
        }
    }

    static bool TypeOk(string type, JsonNode? v) => type switch
    {
        "object" => v is JsonObject,
        "array" => v is JsonArray,
        "string" => v is JsonValue s && s.TryGetValue<string>(out _),
        "boolean" => v is JsonValue b && b.TryGetValue<bool>(out _),
        "integer" => v is JsonValue i && i.TryGetValue<long>(out _),
        "number" => v is JsonValue n && (n.TryGetValue<long>(out _) || n.TryGetValue<double>(out _)) && !n.TryGetValue<string>(out _),
        _ => throw new InvalidOperationException($"schema type '{type}' is not supported by this test's validator"),
    };

    // ---- YAML to JSON (YAML 1.2 core rules: only plain scalars are typed; a date stays a string) ---------------------

    static readonly Regex IntRe = new Regex(@"^[-+]?\d+$");
    static readonly Regex FloatRe = new Regex(@"^[-+]?(\.\d+|\d+(\.\d*)?)([eE][-+]?\d+)?$");

    static JsonNode? ToJson(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode m:
                var o = new JsonObject();
                foreach (var kv in m.Children) o.Add(((YamlScalarNode)kv.Key).Value!, ToJson(kv.Value));
                return o;
            case YamlSequenceNode s:
                return new JsonArray(s.Children.Select(ToJson).ToArray());
            case YamlScalarNode sc:
                var t = sc.Value ?? "";
                if (sc.Style != YamlDotNet.Core.ScalarStyle.Plain) return JsonValue.Create(t);
                if (t == "" || t == "~" || t == "null") return null;
                if (t == "true" || t == "false") return JsonValue.Create(t == "true");
                if (IntRe.IsMatch(t)) return JsonValue.Create(long.Parse(t, CultureInfo.InvariantCulture));
                if (FloatRe.IsMatch(t)) return JsonValue.Create(double.Parse(t, CultureInfo.InvariantCulture));
                return JsonValue.Create(t);
            default:
                throw new InvalidOperationException("unsupported YAML node " + node.GetType().Name);
        }
    }

    static JsonNode? ParseYaml(string text)
    {
        var ys = new YamlStream();
        ys.Load(new StringReader(text));
        return ToJson(ys.Documents[0].RootNode);
    }

    static List<string> Errors(string schemaFile, string yaml)
    {
        var schema = LoadSchema(schemaFile);
        var errors = new List<string>();
        Check(schema, schema, ParseYaml(yaml), "$", errors);
        return errors;
    }

    // ---- every real file passes, and points at its schema ----------------------------------------------------------

    public static IEnumerable<object[]> RealFiles()
    {
        yield return new object[] { "people.yaml", "people.schema.json" };
        yield return new object[] { "offices.yaml", "offices.schema.json" };
        yield return new object[] { "parties.yaml", "parties.schema.json" };
        foreach (var f in Directory.GetFiles(Path.Combine(Root, "events"), "*.yaml").OrderBy(f => f, StringComparer.Ordinal))
            yield return new object[] { "events/" + Path.GetFileName(f), "event.schema.json" };
    }

    [Theory]
    [MemberData(nameof(RealFiles))]
    public void RealContentPassesItsSchema(string file, string schemaFile)
    {
        var errors = Errors(schemaFile, File.ReadAllText(Path.Combine(Root, file)));
        Assert.True(errors.Count == 0, file + ":\n" + string.Join("\n", errors.Take(20)));
    }

    [Theory]
    [MemberData(nameof(RealFiles))]
    public void RealContentNamesItsSchemaInTheFirstLine(string file, string schemaFile)
    {
        var first = File.ReadLines(Path.Combine(Root, file)).First();
        var depth = file.Count(c => c == '/');
        var expected = "# yaml-language-server: $schema=" + string.Concat(Enumerable.Repeat("../", depth)) + (depth == 0 ? "./" : "") + "schema/" + schemaFile;
        Assert.Equal(expected, first);
        Assert.True(File.Exists(Path.Combine(Root, "schema", schemaFile)));
    }

    // ---- the schemas cannot drift from the loader types ------------------------------------------------------------

    static HashSet<string> LoaderFields(Type t) =>
        t.GetProperties().Where(p => p.GetCustomAttribute<YamlIgnoreAttribute>() == null)
            .Select(p => UnderscoredNamingConvention.Instance.Apply(p.Name)).ToHashSet();

    static JsonElement At(JsonElement schema, string pointer)
    {
        var e = schema;
        foreach (var part in pointer.Split('/', StringSplitOptions.RemoveEmptyEntries)) e = e.GetProperty(part);
        return e;
    }

    public static IEnumerable<object[]> TypeMap()
    {
        yield return new object[] { "offices.schema.json", "/definitions/office", typeof(Office) };
        yield return new object[] { "parties.schema.json", "/properties/parties/items", typeof(Party) };
        yield return new object[] { "people.schema.json", "/definitions/person", typeof(Person) };
        yield return new object[] { "people.schema.json", "/definitions/person/properties/portrait", typeof(Portrait) };
        yield return new object[] { "people.schema.json", "/definitions/person/properties/offices/items", typeof(OfficeTerm) };
        yield return new object[] { "people.schema.json", "/definitions/source", typeof(Source) };
        yield return new object[] { "event.schema.json", "", typeof(GameEvent) };
        yield return new object[] { "event.schema.json", "/properties/conditions/items", typeof(EventCondition) };
        yield return new object[] { "event.schema.json", "/properties/requires", typeof(EventRequirement) };
        yield return new object[] { "event.schema.json", "/properties/options/items", typeof(EventOption) };
        yield return new object[] { "event.schema.json", "/properties/options/items/properties/effects/items", typeof(OptionEffect) };
        yield return new object[] { "event.schema.json", "/properties/sources/items", typeof(Source) };
    }

    [Theory]
    [MemberData(nameof(TypeMap))]
    public void SchemaFieldsEqualTheLoaderTypeFields(string schemaFile, string pointer, Type type)
    {
        var node = At(LoadSchema(schemaFile), pointer);
        var fromSchema = node.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.True(fromSchema.SetEquals(LoaderFields(type)),
            $"{schemaFile}{pointer} vs {type.Name}: only in schema [{string.Join(", ", fromSchema.Except(LoaderFields(type)))}], only in loader [{string.Join(", ", LoaderFields(type).Except(fromSchema))}]");
        Assert.True(node.GetProperty("additionalProperties").ValueKind == JsonValueKind.False, "unknown fields must be an error");
        var required = node.TryGetProperty("required", out var req) ? req.EnumerateArray().Select(x => x.GetString()!) : Enumerable.Empty<string>();
        Assert.True(required.All(fromSchema.Contains), "a required name is not a property");
    }

    [Fact]
    public void BiographySourceKeysAndStatusesEqualTheLoader()
    {
        var people = LoadSchema("people.schema.json");
        var keys = At(people, "/definitions/person/properties/biography_sources/properties").EnumerateObject().Select(p => p.Name);
        Assert.Equal(new[] { "born", "died", "name_he", "title_he_form" }, keys.OrderBy(k => k, StringComparer.Ordinal));   // ContentLoader.Validate allows these four
        Assert.Equal(new[] { TitleHeForms.Feminine }, At(people, "/definitions/person/properties/title_he_form/enum").EnumerateArray().Select(x => x.GetString()!));
        var statuses = (HashSet<string>)typeof(ContentLoader).GetField("Statuses", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var inPeople = At(people, "/definitions/person/properties/status/enum").EnumerateArray().Select(x => x.GetString()!);
        var inEvent = At(LoadSchema("event.schema.json"), "/properties/status/enum").EnumerateArray().Select(x => x.GetString()!);
        Assert.True(statuses.SetEquals(inPeople));
        Assert.True(statuses.SetEquals(inEvent));
    }

    [Fact]
    public void TopLevelKeysEqualTheFiles()
    {
        Assert.Equal("offices", Assert.Single(At(LoadSchema("offices.schema.json"), "/properties").EnumerateObject()).Name);
        Assert.Equal("parties", Assert.Single(At(LoadSchema("parties.schema.json"), "/properties").EnumerateObject()).Name);
        Assert.Equal("people", Assert.Single(At(LoadSchema("people.schema.json"), "/properties").EnumerateObject()).Name);
    }

    // ---- the schemas reject the mistakes they exist for ------------------------------------------------------------

    const string GoodPerson = @"
people:
  - id: a_person
    name: A Person
    living: false
    competence: 5
    offices:
      - { office: prime_minister, from: 1948-05-14, sources: [{ url: ""https://example.org"", checked: 2026-10-09 }] }
";

    [Fact]
    public void AGoodPersonPasses() => Assert.Empty(Errors("people.schema.json", GoodPerson));

    [Fact]
    public void AWrongFieldNameInPeopleFails() =>
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("competence", "competance")), e => e.Contains("competance") && e.Contains("unknown field"));

    [Fact]
    public void TitleHeFormIsFeminineOrLeftOut()
    {
        var feminine = GoodPerson.Replace("    living: false\n", "    living: false\n    title_he_form: feminine\n");
        Assert.Empty(Errors("people.schema.json", feminine));
        Assert.NotEmpty(Errors("people.schema.json", feminine.Replace("feminine", "female")));
    }

    [Fact]
    public void AWrongFieldNameInATermFails() =>
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("from:", "form:")), e => e.Contains("form") && e.Contains("unknown field"));

    [Fact]
    public void ATermWithoutSourcesFails() =>
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace(", sources: [{ url: \"https://example.org\", checked: 2026-10-09 }]", "")), e => e.Contains("missing 'sources'"));

    [Fact]
    public void ATermWithEmptySourcesFails() =>
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("[{ url: \"https://example.org\", checked: 2026-10-09 }]", "[]")), e => e.Contains("at least 1"));

    [Fact]
    public void ASourceWithoutACheckedDateFails() =>
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace(", checked: 2026-10-09", "")), e => e.Contains("missing 'checked'"));

    [Fact]
    public void ASourceThatIsNotHttpsFails()
    {
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("https://example.org", "http://example.org")), e => e.Contains("does not match"));
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("https://example.org", "")), e => e.Contains("does not match"));
    }

    [Fact]
    public void LivingIsRequiredAndCompetenceIsBounded()
    {
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("    living: false\n", "")), e => e.Contains("missing 'living'"));
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("competence: 5", "competence: 11")), e => e.Contains("at most"));
        Assert.Contains(Errors("people.schema.json", GoodPerson.Replace("a_person", "A-Person")), e => e.Contains("does not match"));
    }

    [Fact]
    public void AWrongFieldNameInOfficesFails()
    {
        Assert.Empty(Errors("offices.schema.json", "offices:\n  - { id: prime_minister, title: Prime Minister, until: 1951-10-08 }\n"));
        Assert.Contains(Errors("offices.schema.json", "offices:\n  - { id: prime_minister, titel: Prime Minister }\n"), e => e.Contains("titel") && e.Contains("unknown field"));
        Assert.Contains(Errors("offices.schema.json", "offices:\n  - { id: prime_minister }\n"), e => e.Contains("missing 'title'"));
    }

    [Fact]
    public void AnEventWithoutSourcesOrWithATypoFails()
    {
        var good = "id: e\ntitle: T\ntext: X\nfrom: 1950-01-01\noptions:\n  - { id: a, text: A }\nsources: [{ url: \"https://example.org\", checked: 2026-10-09 }]\n";
        Assert.Empty(Errors("event.schema.json", good));
        Assert.Contains(Errors("event.schema.json", good.Replace("sources:", "source:")), e => e.Contains("missing 'sources'") || e.Contains("unknown field"));
        Assert.Contains(Errors("event.schema.json", good.Replace("title:", "titel:")), e => e.Contains("titel"));
        Assert.Contains(Errors("event.schema.json", good.Replace("A }", "A, historcal: true }")), e => e.Contains("historcal"));
        Assert.Contains(Errors("event.schema.json", good.Replace("https://", "http://")), e => e.Contains("does not match"));
    }

    [Fact]
    public void ThePartiesSchemaRejectsATypo() =>
        Assert.Contains(Errors("parties.schema.json", "parties:\n  - { id: mapai, nmae: Mapai }\n"), e => e.Contains("nmae"));

    [Fact]
    public void AnUnsupportedKeywordIsRefusedNotIgnored() =>
        Assert.Throws<InvalidOperationException>(() => Check(JsonDocument.Parse("{\"oneOf\": []}").RootElement, default, JsonValue.Create("x"), "$", new List<string>()));
}
