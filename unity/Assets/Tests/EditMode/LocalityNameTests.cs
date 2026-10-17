#nullable enable
using System.Collections;
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
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Issue #245: the info panel lists a hex's 1948 localities, with today's name by the same rule as the map tooltip
/// (Localities.TodayName: a living place carries on, owner ruling 2026-10-12).</summary>
public class LocalityNameTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    MapScene map = null!;
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

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
        var p = map.Localities.First(x => Localities.TodayName(x, true) != null && !string.IsNullOrEmpty(x.TodayHe) && x.TodayHe != x.NameHe);   // #899 drops a today name equal to the Hebrew 1948 name
        var text = InfoPanelPresenter.LocalitiesText(He, new[] { p });
        StringAssert.Contains(p.TodayHe!, text);
        Assert.AreEqual("", InfoPanelPresenter.LocalitiesText(He, new Localities.Place[0]));
    }

    /// <summary>#899: under Hebrew each 1948 locality is named by its name_he (Beisan is ביסאן), and by the English name where the
    /// row has none (Na'ana). Where the Hebrew 1948 name and the Hebrew today name are one word (Umm el Fahm and Umm al-Fahm
    /// are both אום אל-פחם: 48 rows that would otherwise show a today name), the "today" part would repeat the name, so it is left out. English is unchanged.</summary>
    [Test]
    public void TheHebrewPanelNamesEach1948LocalityByItsHebrewName()
    {
        var by = map.Localities.ToDictionary(p => p.Settlement.Id);
        var (beisan, naana, fahm) = (by["beit_shean"], by["naana"], by["umm_al_fahm"]);
        Assert.IsNull(naana.NameHe, "Na'ana has no Hebrew name");
        Assert.AreEqual(fahm.NameHe, fahm.TodayHe, "one Hebrew word for 1948 and today");
        Assert.AreEqual("יישובים ב-1948: ביסאן (היום: בית שאן)", InfoPanelPresenter.LocalitiesText(He, new[] { beisan }));
        Assert.AreEqual(He.Format("info.localities", "Na'ana"), InfoPanelPresenter.LocalitiesText(He, new[] { naana }));   // Format marks the Latin run
        Assert.AreEqual("יישובים ב-1948: אום אל-פחם", InfoPanelPresenter.LocalitiesText(He, new[] { fahm }));
        Assert.AreEqual("1948 localities: Beisan (today: Beit She'an), Na'ana, Umm el Fahm (today: Umm al-Fahm)",
            InfoPanelPresenter.LocalitiesText(En, new[] { beisan, naana, fahm }));
    }

    /// <summary>The list wraps inside the info panel: the hex with the most localities (seven around Tel Aviv) at a desktop,
    /// a laptop and a phone size, on the real Game.uxml and PanelSettings. Unwrapped, the line ran far past the panel.</summary>
    [UnityTest]
    public IEnumerator TheLongestListWrapsInsideThePanel()
    {
        var at = GameRoot.LocalitiesAt(map);
        var hex = map.Localities.Select(p => HexPicker.FromLonLat(p.Settlement.Lon, p.Settlement.Lat, map.Hexes.bounds, map.Hexes.hex_size_km))
            .OrderByDescending(h => at(h.q, h.r).Count).First();
        Assert.GreaterOrEqual(at(hex.q, hex.r).Count, 5);
        foreach (var (w, h) in new[] { (1920, 1080), (960, 600), (390, 844) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LocalityLayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var info = new InfoPanelPresenter(root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En), En,
                C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id), localitiesAt: at);
            try
            {
                info.Show(hex.q, hex.r);
                var label = root.Q<Label>("info-localities");
                for (int i = 0; i < 20 && !(label.contentRect.width > 0); i++) yield return null;
                yield return null;   // one more pass, so text measured after the first layout settles
                var box = label.contentRect;
                var natural = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                Assert.Greater(natural.x, box.width, $"{w}x{h}: the test needs a line longer than the panel");
                Assert.AreEqual(WhiteSpace.Normal, label.resolvedStyle.whiteSpace, $"{w}x{h}: the list wraps");
                var wrapped = label.MeasureTextSize(label.text, box.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(wrapped.y, box.height + 1f, $"{w}x{h}: the wrapped list fits its box {box}");
                var panel = root.Q("info-panel").worldBound;
                Assert.LessOrEqual(label.worldBound.xMax, panel.xMax + 0.5f, $"{w}x{h}: inside the panel {panel}");
            }
            finally
            {
                info.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }
}
