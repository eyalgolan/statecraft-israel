#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>#41, the keyboard-only turn's "Still open" list, item 2, the case where the press leaves its whole part with
/// nothing that can take the focus. One Build order on a site sets `taken`, which disables every Build in that part
/// (`GddHexPanel`, `SetEnabled(!ordered &amp;&amp; !taken)`), so the part is then a state line and disabled buttons. Stop on a
/// site or a corridor segment and a project's Cancel have the same shape. Without a last stop outside the part the focus
/// is left nowhere and the next Tab starts again at the first screen. Harness as `CityPanelTests`: `UiTestKit` gives the
/// tree a real panel, so focus and Submit both work.</summary>
public class SitePanelFocusTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly System.Collections.Generic.List<Settlement> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    const string City = "tel_aviv", Site = "mefalsim";

    static (int q, int r) CityHex => C.SettlementHexes.Where(x => x.Id == City).Select(x => (x.Q, x.R)).Single();
    static Statecraft.Sim.Site SiteDef => C.Sites.Single(x => x.Id == Site);

    static SimBridge Bridge(UiText text, Action<GameState>? edit = null)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        edit?.Invoke(g.State);
        return new SimBridge(g, text);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge, UiText text) =>
        new InfoPanelPresenter(ui.Root, bridge, text, Towns.ToDictionary(t => t.Id, t => t.Name));

    static int AddHalutz(GameState s)
    {
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", CityHex.q, CityHex.r));
        return id;
    }

    [Test]
    public void BuildKeepsTheFocusWhenItDisablesEveryBuildInThePart()
    {
        using var ui = new UiTestKit();
        int a = -1, b = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); b = AddHalutz(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);

        var first = ui.Q<Button>($"site-build-{a}");
        Assert.IsTrue(first.enabledSelf, "precondition: Build is enabled before the order");
        first.Focus();
        Assert.AreEqual(first, ui.Root.panel.focusController.focusedElement, "precondition: Build has the focus");

        UiTestKit.Press(first);

        // One order takes the site, so every Build in the part is disabled and nothing in it can hold the focus.
        Assert.IsFalse(ui.Root.Q<Button>($"site-build-{a}")?.enabledSelf ?? false, "precondition: the pressed Build is disabled");
        Assert.IsFalse(ui.Root.Q<Button>($"site-build-{b}")?.enabledSelf ?? false, "precondition: the other group's Build is disabled too");

        var now = ui.Root.panel.focusController.focusedElement as VisualElement;
        Assert.IsNotNull(now, "the focus is not dropped when the whole part is disabled");
        Assert.IsTrue(now!.enabledInHierarchy && now.focusable, $"and it is on something that can hold it, not {now.name}");
    }
}
