#nullable enable
using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;

public class RegionNamesTests
{
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static MapInputs Inputs => GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
    static MapRegions Regions(MapInputs m) => new MapRegions(Subdistricts1948.Parse(m.RegionsYaml),
        Armistice1949.Parse(m.ArmisticeYaml), names: MapRegionNames.Parse(m.RegionNamesYaml));
    // Independent primary-scan transcription, not the production catalogue or Names.Pick.
    // Gazette 1415, Supplement 2, 7 June 1945, pp. 493-499 / ISA segment scans 122-128.
    [TestCase("acre", "נפת עכו", "מחוז הגליל")]
    [TestCase("beisan", "נפת בית־שאן", "מחוז הגליל")]
    [TestCase("nazareth", "נפת נצרת", "מחוז הגליל")]
    [TestCase("safad", "נפת צפת", "מחוז הגליל")]
    [TestCase("tiberias", "נפת טבריה", "מחוז הגליל")]
    [TestCase("haifa", "נפת חיפה", "מחוז חיפה")]
    [TestCase("jenin", "נפת ג'נין", "מחוז שומרון")]
    [TestCase("nablus", "נפת שכם", "מחוז שומרון")]
    [TestCase("tulkarm", "נפת טול כרם", "מחוז שומרון")]
    [TestCase("hebron", "נפת חברון", "מחוז ירושלים")]
    [TestCase("jerusalem", "נפת ירושלים", "מחוז ירושלים")]
    [TestCase("ramallah", "נפת ראם־אללה", "מחוז ירושלים")]
    [TestCase("jaffa", "נפת יפו", "מחוז לוד")]
    [TestCase("ramle", "נפת רמלה", "מחוז לוד")]
    [TestCase("gaza", "נפת עזה", "מחוז עזה")]
    [TestCase("beersheba", "נפת באר־שבע", "מחוז עזה")]
    public void EverySubdistrictHasItsOwnIdentity(string id, string name, string district)
    {
        var m = Regions(Inputs);
        var r = m.Subdistricts.Regions.Single(x => x.Key == id);
        var h = m.Subdistricts.Layer.Hexes.First(x => m.Subdistricts.Layer.RegionOf(x.q, x.r) == r.Id);
        var date = new DateTime(1948, 5, 15);
        Assert.IsTrue(string.Equals(Text(true).Format("info.region", name, district), InfoPanelPresenter.RegionText(m, Text(true), h.q, h.r, date), StringComparison.Ordinal));
        Assert.AreEqual(Text(false).Format("info.region", r.Name, r.District), InfoPanelPresenter.RegionText(m, Text(false), h.q, h.r, date));
    }

    // Fixture is synthetic, not a historical translation or sign-off. Tests the human-removal workflow separately.
    const string Fixture = "names:\n  - key: subdistrict.acre.name\n    name: Test\n    name_he: בדיקה\n    name_he_sources:\n      - url: https://example.org/primary\n        checked: 2026-10-26\n        cite: page 1\n";
    [Test]
    public void AbsentReviewMarkerIsAcceptedAfterHumanReview() =>
        Assert.IsTrue(string.Equals("בדיקה", MapRegionNames.Parse(Fixture).Pick("subdistrict.acre.name", "Acre", true), StringComparison.Ordinal));
    [Test]
    public void ExistingEditorPendingReviewMarkerIsAccepted() => Assert.DoesNotThrow(() => MapRegionNames.Parse(Fixture + "    name_he_review: agent-checked\n"));
    [Test]
    public void PendingReviewMarkerIsAccepted() => Assert.DoesNotThrow(() => MapRegionNames.Parse(Fixture + "    name_he_review: native-review-pending\n"));
    [TestCase("")]
    [TestCase("null")]
    [TestCase("reviewed")]
    public void ExplicitBlankNullOrOtherReviewStateIsRejected(string state) =>
        Assert.Throws<InvalidDataException>(() => MapRegionNames.Parse(Fixture + "    name_he_review: " + state + "\n"));
    [TestCase("https://example.org/primary", "")]
    [TestCase("https://example.org/primary", "http://example.org/primary")]
    [TestCase("2026-10-26", "yesterday")]
    [TestCase("page 1", "")]
    public void SourceUrlCheckedDateAndPageCitationAreRequired(string old, string value) =>
        Assert.Throws<InvalidDataException>(() => MapRegionNames.Parse((Fixture + "    name_he_review: native-review-pending\n").Replace(old, value)));
    [Test]
    public void MissingSourceIsRefusedWithFieldIdentity()
    {
        var yaml = "names:\n  - key: subdistrict.acre.name\n    name: Test\n    name_he: בדיקה\n    name_he_review: native-review-pending\n";
        StringAssert.Contains("subdistrict.acre.name", Assert.Throws<InvalidDataException>(() => MapRegionNames.Parse(yaml))!.Message);
    }
    [Test]
    public void RemovingARealCatalogSourceIsRefused()
    {
        var yaml=Inputs.RegionNamesYaml;
        var start=yaml.IndexOf("    name_he_sources:",StringComparison.Ordinal);
        var end=yaml.IndexOf("  - key:",start,StringComparison.Ordinal);
        Assert.Greater(end,start);
        var mutated=yaml.Remove(start,end-start);
        StringAssert.Contains("subdistrict.acre.name",Assert.Throws<InvalidDataException>(()=>MapRegionNames.Parse(mutated))!.Message);
    }
    [Test]
    public void DuplicateAndUnknownTargetsAreRejected()
    {
        var row = Fixture.Substring("names:\n".Length) + "    name_he_review: native-review-pending\n";
        Assert.Throws<InvalidDataException>(() => MapRegionNames.Parse("names:\n" + row + row));
        var m = Inputs;
        Assert.Throws<InvalidDataException>(() => new MapRegions(Subdistricts1948.Parse(m.RegionsYaml), Armistice1949.Parse(m.ArmisticeYaml),
            names: MapRegionNames.Parse(("names:\n" + row).Replace("subdistrict.acre.name", "subdistrict.typo.name"))));
    }

    static readonly string[] Gaps = {
        "zone.syria_dmz_north.name", "zone.syria_dmz_south.name",
        "line.egypt_mv1950_demarcation.name",
        "line.egypt_mv1950_zone_lines.name",
        "line.jordan_adl_iraqi_c_east.name", "line.jordan_adl_iraqi_b_wadi_ara.name", "line.jordan_adl_iraqi_a_triangle.name",
        "line.jordan_adl_iraqi_c_south.name", "line.jordan_adl_a_latrun.name", "line.jordan_adl_bc_south.name", "line.jordan_adl_d_arava.name"
    };
    static readonly Dictionary<string, string> ArmisticeIdentities = new Dictionary<string, string> {
        {"line.egypt_mv1950_demarcation.label_note", "פשרת שעה, 1950"}, {"line.egypt_mv1950_zone_lines.label_note", "פשרת שעה, 1950"},
        {"subdistrict.label", "הגלילות האדמיניסטרטיביים"}, {"armistice.label", "קווי שביתת הנשק, 1949"},
        {"front.egypt.name", "מצרים"}, {"front.lebanon.name", "לבנון"}, {"front.jordan.name", "ירדן"}, {"front.syria.name", "סוריה"},
        {"zone.egypt_el_auja_dmz.name", "שטח הכפר אל־עוג'ה וסביבתו"},
        {"line.lebanon_adl.name", "קו שביתת הנשק: הגבול הבין־לאומי"}, {"line.syria_adl.name", "קו שביתת הנשק"},
        // Owner-approved captions, 27 Oct 2026 (signed-map reconciliation still open).
        {"zone.syria_dmz_central.name", "האזור המפורז (ישראל–סוריה), החלק המרכזי: דרדרה, הירדן ומשמר־הירדן"},
        {"line.egypt_adl_gaza.name", "קו שביתת הנשק (מצרים–ישראל) של 1949, גזרת עזה־רפיח: מוצא ודי חזי עד עזה"}
    };
    static string Agreement(string front) => front switch {
        "egypt" => "הסכם מצרים-ישראל על שביתת נשק כללית",
        "lebanon" => "הסכם לבנון-ישראל על שביתת נשק כללית",
        "jordan" => "הסכם ממלכת הירדן ההאשמית-ישראל על שביתת נשק כללית",
        "syria" => "הסכם ישראל-סוריה על שביתת נשק כללית",
        _ => throw new AssertionException("unreviewed front " + front)
    };
    [Test]
    public void EveryArmisticeFieldHasAnIndependentSourcedIdentityOrAnExplicitResearchGap()
    {
        var m = Regions(Inputs); var expected = new Dictionary<string, string>(ArmisticeIdentities);
        var all = new Dictionary<string, string> { {"subdistrict.label", m.Subdistricts.Label}, {"armistice.label", m.Armistice!.Label} };
        foreach (var f in m.Armistice.Fronts) all.Add("front." + f.Key + ".name", f.Name);
        foreach (var (kind, items) in new[] { ("zone", m.Armistice.Zones), ("line", m.Armistice.Lines) })
            foreach (var i in items)
            {
                var prefix = kind + "." + i.Key + ".";
                all.Add(prefix + "name", i.Name); all.Add(prefix + "agreement", i.Agreement);
                if (i.LabelNote != "") all.Add(prefix + "label_note", i.LabelNote);
                if (!Gaps.Contains(prefix + "agreement")) expected.Add(prefix + "agreement", i.Key.StartsWith("egypt_mv1950_",StringComparison.Ordinal) ? "פשרת שעה להסכם שביתת הנשק בין ישראל ומצרים" : Agreement(i.Front.Key));
            }
        CollectionAssert.AreEquivalent(all.Keys, expected.Keys.Concat(Gaps), "Every displayed field must be independently supplied or explicitly tracked; not a non-English inequality oracle.");
        foreach (var (key, value) in expected) Assert.IsTrue(string.Equals(value, m.Names.Pick(key, all[key], true), StringComparison.Ordinal), key);
        var followup = File.ReadAllText(Path.Combine(Paths.RepoRoot, "docs", "map-region-names.md"));
        foreach (var key in Gaps)
        {
            Assert.AreEqual(all[key], m.Names.Pick(key, all[key], true), key + ": keep English until sourced");
            StringAssert.Contains("`" + key + "`", followup);
        }
        StringAssert.Contains("NEEDS-RESEARCH", followup);
        // All newly supplied rows remain pending; parser support for removal is not a human sign-off.
        Assert.AreEqual(59, Inputs.RegionNamesYaml.Split(new[] {"    name_he_review: native-review-pending"}, StringSplitOptions.None).Length - 1);
    }

    [Test]
    public void EmptyCatalogAndOlderBundlePreservePerFieldEnglishFallback()
    {
        var input = Inputs; var sub = Subdistricts1948.Parse(input.RegionsYaml); var arm = Armistice1949.Parse(input.ArmisticeYaml);
        var plain = new MapRegions(sub, arm); var m = Regions(input);
        Assert.AreEqual("fallback", MapRegionNames.Parse("").Pick("subdistrict.acre.name", "fallback", true));
        foreach (var date in new[] {new DateTime(1948,5,15), new DateTime(1949,2,24), new DateTime(1949,7,20), new DateTime(1950,3,1)})
        {
            Assert.AreEqual(plain.Label(date, Text(false).Format), m.Label(date, Text(false).Format));
            foreach (var h in sub.Layer.Hexes)
            {
                Assert.AreEqual(InfoPanelPresenter.RegionText(plain, Text(false), h.q, h.r, date), InfoPanelPresenter.RegionText(m, Text(false), h.q, h.r, date));
                // With the catalogue omitted, Hebrew UI patterns still use the original English display fields.
                if (date.Year == 1948 && sub.SubdistrictAt(h.q,h.r) is Subdistricts1948.Region r)
                    Assert.IsTrue(string.Equals(Text(true).Format("info.region",r.Name,r.District), InfoPanelPresenter.RegionText(plain,Text(true),h.q,h.r,date), StringComparison.Ordinal));
            }
        }
    }
    [Test]
    public void ArmisticePresentersUsePerFieldNamesWithoutChangingDatesWarningsOrNotes()
    {
        var m = Regions(Inputs); var he = Text(true); var date = new DateTime(1950,3,1);
        // Wiring fixture uses unique synthetic Hebrew strings, independent of the historical content.
        var keys = new List<string> {"subdistrict.label", "armistice.label"};
        foreach (var f in m.Armistice!.Fronts) keys.Add("front."+f.Key+".name");
        foreach (var (kind,items) in new[] {("zone",m.Armistice.Zones),("line",m.Armistice.Lines)})
            foreach(var i in items) foreach(var field in new[]{"name","agreement","label_note"}) keys.Add(kind+"."+i.Key+"."+field);
        var replacements = keys.Select((k,i)=>(k,v:"בדיקה "+i.ToString(CultureInfo.InvariantCulture))).ToDictionary(x=>x.k,x=>x.v);
        var catalog = "names:\n" + string.Concat(replacements.Select(x=>Fixture.Substring("names:\n".Length)
            .Replace("subdistrict.acre.name",x.Key).Replace("name_he: בדיקה", "name_he: "+x.Value)+"    name_he_review: native-review-pending\n"));
        var synthetic = new MapRegions(m.Subdistricts,m.Armistice,names:MapRegionNames.Parse(catalog));
        string Date(DateTime d)=>"\u2067"+Control1948.DateText(d,k=>he[k])+"\u2069";
        foreach(var zone in synthetic.ZonesOn(date)) foreach(var h in synthetic.Armistice!.HexesOf(zone))
            Assert.IsTrue((InfoPanelPresenter.RegionText(synthetic,he,h.q,h.r,date)).IndexOf(he.Format("info.armistice_zone",replacements["zone."+zone.Key+".name"],replacements["zone."+zone.Key+".agreement"],Date(zone.Signed)), StringComparison.Ordinal) >= 0);
        foreach(var h in synthetic.Subdistricts.Layer.Hexes) foreach(var line in synthetic.LinesAt(h.q,h.r,date))
            Assert.IsTrue((InfoPanelPresenter.RegionText(synthetic,he,h.q,h.r,date)).IndexOf(he.Format(line.LowAccuracy?"info.armistice_line_low":"info.armistice_line",replacements["line."+line.Key+".name"],replacements["line."+line.Key+".agreement"],Date(line.Signed)), StringComparison.Ordinal) >= 0);
        var label=synthetic.Label(date,he.Format,true);
        Assert.IsTrue((label).IndexOf(replacements["subdistrict.label"], StringComparison.Ordinal) >= 0); Assert.IsTrue((label).IndexOf(replacements["armistice.label"], StringComparison.Ordinal) >= 0);
        Assert.IsTrue((label).IndexOf(he.Format("map.layer_front_low",replacements["front.jordan.name"]), StringComparison.Ordinal) >= 0);
        Assert.IsTrue((label).IndexOf(replacements["line.egypt_mv1950_demarcation.label_note"], StringComparison.Ordinal) >= 0);
        // The sourced 1950 instrument note remains visible; its geometry and date are unchanged.
        Assert.IsTrue((m.Label(date,he.Format,true)).IndexOf("פשרת שעה, 1950", StringComparison.Ordinal) >= 0);
        Assert.IsTrue((m.Label(date,he.Format,true)).IndexOf("1950 modus vivendi", StringComparison.Ordinal) < 0);
        Assert.IsTrue((m.Label(date,he.Format,true)).IndexOf(he.Format("map.layer_front_low","ירדן"), StringComparison.Ordinal) >= 0);
        foreach(var line in m.Armistice.Lines)
        {
            var hex=m.Armistice.LineHexes.First(kv=>kv.Value.Contains(line)).Key;
            var name=ArmisticeIdentities.TryGetValue("line."+line.Key+".name",out var supplied)?supplied:line.Name;
            var agreement=line.Key.StartsWith("egypt_mv1950_",StringComparison.Ordinal)?"פשרת שעה להסכם שביתת הנשק בין ישראל ומצרים":Agreement(line.Front.Key);
            var expected=he.Format(line.LowAccuracy?"info.armistice_line_low":"info.armistice_line",name,agreement,Date(line.Signed));
            Assert.IsTrue((InfoPanelPresenter.RegionText(m,he,hex.q,hex.r,line.ShownFrom.AddDays(-1))).IndexOf(expected, StringComparison.Ordinal) < 0);
            Assert.IsTrue((InfoPanelPresenter.RegionText(m,he,hex.q,hex.r,line.ShownFrom)).IndexOf(expected, StringComparison.Ordinal) >= 0);
        }
    }
    [Test]
    public void CatalogShipsAndBothBootPathsCarryItWithoutChangingSimHash()
    {
        var files=ContentManifest.Collect(Paths.Content);
        CollectionAssert.Contains(files,"map/"+MapRegionNames.FileName);
        var texts=files.ToDictionary(f=>f,f=>File.ReadAllText(Path.Combine(Paths.Content,f)));
        var m=Inputs; var with=GameInputs.FromTexts(texts,files,m.HeightmapJson,m.HeightmapR16z);
        Assert.AreEqual(m.RegionNamesYaml,with.Map.RegionNamesYaml);
        texts.Remove("map/"+MapRegionNames.FileName);
        var old=GameInputs.FromTexts(texts,files.Where(f=>f!="map/"+MapRegionNames.FileName),m.HeightmapJson,m.HeightmapR16z);
        Assert.IsEmpty(old.Map.RegionNamesYaml);
        Assert.AreEqual(with.Content.Hash,old.Content.Hash,"map-only presentation must not affect deterministic simulation hash");
    }
}
