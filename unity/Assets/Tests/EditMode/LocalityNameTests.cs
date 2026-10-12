#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Issue #245: the info panel lists a hex's 1948 localities, with today's name by the same rule as the map tooltip
/// (Localities.TodayName: a living place carries on, owner ruling 2026-10-12).</summary>
public class LocalityNameTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    MapScene map = null!;
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings.Replace("strings.en.yaml", "strings.he.yaml")).text, "strings.he.yaml");

    [SetUp]
    public void Open() { EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single); map = Object.FindFirstObjectByType<MapScene>(); map.Build(); }

    [TearDown]
    public void Close() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    InfoPanelPresenter Panel(UiTestKit ui, UiText text)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        return new InfoPanelPresenter(ui.Root, new SimBridge(g, text), text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id), localitiesAt: GameRoot.LocalitiesAt(map));
    }

    [Test]
    public void TheRuleIsOneFunctionForTheTooltipAndThePanel()
    {
        var by = map.Localities.ToDictionary(p => p.Settlement.Id);
        Assert.AreEqual("Beit She'an", Localities.TodayName(by["beit_shean"], false));
        Assert.IsNull(Localities.TodayName(by["isdud"], false), "a place that did not carry on");
        Assert.IsNull(Localities.TodayName(by["nablus"], false), "Nablus is Nablus");
        foreach (var p in map.Localities)
        {
            var tip = MapLabelsPresenter.TipOf(p, n => "T:" + n, "M");
            var today = Localities.TodayName(p, false);
            Assert.AreEqual(today != null, tip != null && tip.Contains("T:" + today), p.Settlement.Id + ": tooltip and rule agree");
        }
    }

    [Test]
    public void TheInfoPanelListsTheHexsLocalitiesWithTodaysNameOnlyWhereTheRuleAllows()
    {
        var at = GameRoot.LocalitiesAt(map);
        var withToday = map.Localities.Where(p => Localities.TodayName(p, false) != null).ToList();
        var without = map.Localities.Where(p => Localities.TodayName(p, false) == null).ToList();
        Assert.IsNotEmpty(withToday); Assert.IsNotEmpty(without);
        using var ui = new UiTestKit();
        var info = Panel(ui, En);
        var label = ui.Q<Label>("info-localities");

        var p1 = withToday[0];
        var (q, r) = HexPicker.FromLonLat(p1.Settlement.Lon, p1.Settlement.Lat, map.Hexes.bounds, map.Hexes.hex_size_km);
        info.Show(q, r);
        Assert.AreEqual(DisplayStyle.Flex, label.style.display.value);
        StringAssert.StartsWith("1948 localities: ", label.text);
        StringAssert.Contains($"{p1.Settlement.Name} (today: {Localities.TodayName(p1, false)})", label.text);
        foreach (var p in at(q, r))
        {
            StringAssert.Contains(p.Settlement.Name, label.text, "every locality of the hex is listed");
            if (Localities.TodayName(p, false) == null) StringAssert.DoesNotContain(p.Settlement.Name + " (", label.text, p.Settlement.Id + " has no today name");
        }

        var p2 = without[0];
        var (q2, r2) = HexPicker.FromLonLat(p2.Settlement.Lon, p2.Settlement.Lat, map.Hexes.bounds, map.Hexes.hex_size_km);
        info.Show(q2, r2);
        StringAssert.Contains(p2.Settlement.Name, label.text);
        StringAssert.DoesNotContain(p2.Settlement.Name + " (", label.text, "a place that did not carry on shows no today name");
    }

    [Test]
    public void AHexWithNoLocalitiesShowsNoSection()
    {
        var at = GameRoot.LocalitiesAt(map);
        var hex = C.SettlementHexes.Select(h => (h.Q, h.R)).First(h => at(h.Q, h.R).Count == 0);
        using var ui = new UiTestKit();
        var info = Panel(ui, En);
        var label = ui.Q<Label>("info-localities");
        info.Show(hex.Q, hex.R);
        Assert.AreEqual(DisplayStyle.None, label.style.display.value);
        Assert.AreEqual("", label.text);
        var p = map.Localities[0];
        var (q, r) = HexPicker.FromLonLat(p.Settlement.Lon, p.Settlement.Lat, map.Hexes.bounds, map.Hexes.hex_size_km);
        info.Show(q, r);
        Assert.AreEqual(DisplayStyle.Flex, label.style.display.value);
        info.Show(hex.Q, hex.R);
        Assert.AreEqual(DisplayStyle.None, label.style.display.value, "the section goes when the next hex has none");
    }

    [Test]
    public void TheHebrewPanelUsesHebrewTodayNames()
    {
        var p = map.Localities.First(x => Localities.TodayName(x, true) != null && !string.IsNullOrEmpty(x.TodayHe));
        var text = InfoPanelPresenter.LocalitiesText(He, new[] { p });
        StringAssert.Contains(p.TodayHe!, text);
        Assert.AreEqual("", InfoPanelPresenter.LocalitiesText(He, new Localities.Place[0]));
    }
}
