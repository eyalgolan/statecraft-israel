#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Spec 7.3: Show me draws the gold ring round the named part of the real HUD, shows a unit or a city as a click
/// would and puts the selection back after, moves the popup off its target, and never carries out the advised action.</summary>
public class AdvisorShowMeTests
{
    const string Catalog = "advisors:\n  - { id: ben_gurion, person: ben_gurion }\n  - { id: meyerson, person: golda_meir }\n  - { id: dayan, person: dayan }\n" +
        "messages:\n" +
        "  - { id: t.att, advisor: ben_gurion, signal: new_game, level: new, actions: [{ show_me: attention }] }\n" +
        "  - { id: t.city, advisor: meyerson, signal: turn_ended, level: new, actions: [{ show_me: city_housing }] }\n";

    static UiText Text() => UiText.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text +
        "\nadvisor.t.att.text: \"Attention.\"\nadvisor.t.city.text: \"A city.\"\n");

    /// <summary>Find returns only what is shown with a size, so the HUD is laid out first (the layout harness).</summary>
    [UnityTest]
    public IEnumerator EveryNameIsKnownAndEverySideMenuButtonHasOne()
    {
        foreach (var s in ScreenNav.Screens) CollectionAssert.Contains(AdvisorTargets.Names, AdvisorTargets.Nav(s));
        yield return AdvisorPopupLayoutTests.Shown(1280, 720, false, (root, advisor) =>
        {
            foreach (var (name, element) in new[] { ("attention", "attention"), ("nav_objectives", "nav-objectives"), ("nav_budget", "nav-budget") })
                Assert.AreEqual(element, AdvisorTargets.Find(root.Q("root") ?? root, name, null).Single().name, name);
        });
    }

    [Test]
    public void ShowMeOnACityShowsItAndPutsTheSelectionBackWhenDone()
    {
        var calls = new List<string>();
        using var rig = new AdvisorPopupTests.Rig(catalog: Catalog, text: Text(), hooks: new AdvisorHooks
        {
            ShowCity = () => calls.Add("city"), KeepSelection = () => { calls.Add("keep"); return () => calls.Add("back"); },
        });
        rig.Advisor.NewGame(); rig.Ui.Press("advisor-got-it");
        rig.EndTurn();
        Assert.AreEqual("t.city", rig.Advisor.Shown!.Id, "precondition");
        rig.Ui.Press("advisor-show-me");
        CollectionAssert.AreEqual(new[] { "keep", "city" }, calls);
        Assert.IsTrue(rig.HudRoot.ClassListContains(AdvisorPresenter.RingClass));
        rig.Ui.Press("advisor-got-it");
        CollectionAssert.AreEqual(new[] { "keep", "city", "back" }, calls, "Got it puts back what Show me found");
        Assert.IsFalse(rig.HudRoot.ClassListContains(AdvisorPresenter.RingClass));
    }

    /// <summary>Review Focus 5 (desktop half): a target the popup's top would cover sends the popup to its slot's foot.</summary>
    [UnityTest]
    public IEnumerator AShowMeTargetUnderThePopupIsUncovered()
    {
        yield return AdvisorPopupLayoutTests.Shown(1280, 720, false, (root, advisor) =>
        {
            var slot = advisor.Slot();
            var target = new Rect(slot.xMin + 20, slot.yMin + 10, 60, 30);     // a map target in the slot's upper part
            var popup = advisor.Place(target)!.Value;
            Assert.IsFalse(popup.Overlaps(target), $"{popup} covers {target}");
            Assert.LessOrEqual(popup.yMax, slot.yMax + 0.5f);
        });
    }

    [UnityTest]
    public IEnumerator TheRingGoesRoundAttentionInsideTheWindow()
    {
        yield return AdvisorPopupLayoutTests.Shown(1280, 720, false, (root, advisor) =>
        {
            advisor.ShowTarget(AdvisorTargets.Attention);             // places the ring at once (its style; the layout follows)
            var s = root.Q("advisor-ring").style;
            var ring = new Rect(s.left.value.value, s.top.value.value, s.width.value.value, s.height.value.value);
            var layer = root.Q(AdvisorPresenter.LayerName); var a = root.Q("attention").worldBound;
            var target = Rect.MinMaxRect(layer.WorldToLocal(a.min).x, layer.WorldToLocal(a.min).y, layer.WorldToLocal(a.max).x, layer.WorldToLocal(a.max).y);
            Assert.IsTrue(ring.Contains(target.min) && ring.Contains(target.max), $"{ring} round {target}");
            Assert.IsTrue(new Rect(Vector2.zero, layer.layout.size).Contains(ring.max), "inside the window");
        });
    }
}
