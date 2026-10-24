#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The capture and end-of-occupation dialog (GDD Phase 6 PR 10, #802; plan 2026-10-15-gdd-phase-6.md, PR 10): when a
/// settlement's record awaits the player's choice, or the end of its occupation, the dialog names the place and lists exactly
/// the options SettlementControl.CaptureOptions or EndOptions lists, in its order, each with the sim's preview or, refused,
/// disabled with its reason in the UI's language; nothing is pre-selected; a choice queues ResolveCapture or EndOccupation
/// through SimBridge; the places go in one ordinal pass (owner, 24 October 2026, "By settlement order"); Escape hides it
/// without choosing; it plays no sound and shows no video; a decision card goes first. The
/// target rows here are test fixtures on the real content (Lydda and Ramle, which share a hex), with a made-up operation
/// line (the content requires one; the dialog does not show it): the real target list ships empty until its research and
/// review are done.</summary>
public class CaptureDialogTests
{
    const string Operation = "Fixture operation (a test value, not a source)";

    /// <summary>The real content with Lydda and Ramle as targets; <paramref name="price"/> sets attention_cost.resolve_capture
    /// and <paramref name="endPrice"/> attention_cost.end_occupation (0 in the content), so the sim's Attention preview has a
    /// number to show.</summary>
    static Content Targets(double price = 0, double endPrice = 0)
    {
        var dir = Paths.Content;
        var events = Directory.GetFiles(Path.Combine(dir, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList();
        string Row(string id) => $"  - {{ id: {id}, q: -3, r: 21, operation: \"{Operation}\", sources: [{{ url: \"https://example.org/{id}\", checked: 2026-10-21 }}], residents: stayed, residents_sources: [{{ url: \"https://example.org/{id}/residents\", checked: 2026-10-21 }}], annexation: {{ people: {{ value: 60, status: sourced, date: 1948-11-08, sources: [{{ url: \"https://example.org/{id}/people\", checked: 2026-10-21 }}] }}, employed_share: {{ value: 0.5, status: design }} }}, status: draft }}\n";   // stayed, so the dialog's lines are the ones it tests (#799)
        return ContentLoader.Load(rel =>
        {
            var path = Path.Combine(dir, rel);
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            if (rel == "settlement_targets.yaml") return "targets:\n" + Row("lod") + Row("ramla");
            if (rel == "rules.yaml" && price != 0)
            {
                Assert.IsTrue(text.Contains("  resolve_capture: 0 "), "rules.yaml prices resolve_capture at 0");
                text = text.Replace("  resolve_capture: 0 ", "  resolve_capture: " + price.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ");
            }
            if (rel == "rules.yaml" && endPrice != 0)
            {
                Assert.IsTrue(text.Contains("  end_occupation: 0 "), "rules.yaml prices end_occupation at 0");
                text = text.Replace("  end_occupation: 0 ", "  end_occupation: " + endPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ");
            }
            return text;
        }, events);
    }

    static readonly Content C = Targets();
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>A new game with no event pending and each of <paramref name="places"/> captured and awaiting the choice.</summary>
    static GameSession Awaiting(Content c, params string[] places)
    {
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        g.State.PendingEvents.Clear();
        foreach (var id in places) g.State.Settlements[id] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, g.State.Date);
        return g;
    }

    /// <summary>A new game with no event pending and each of <paramref name="places"/> awaiting the end of its occupation
    /// (R-CTRL-012), <paramref name="fled"/> its record's flag; with <paramref name="garrison"/>, an Israeli soldier on Lydda
    /// and Ramle's hex, the unit Annex and Withdraw need (R-CTRL-014).</summary>
    static GameSession Ending(Content c, bool garrison, bool fled, params string[] places)
    {
        var g = Awaiting(c);
        foreach (var id in places) g.State.Settlements[id] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, g.State.Date, fled: fled);
        if (garrison) { var u = g.State.NextId++; g.State.Units.Add(u, new Unit(u, "veteran_soldier", -3, 21, side: Movement.PlayerSide, strength: 3)); }
        return g;
    }

    /// <summary>The settlements' Hebrew names as GameRoot hands them over (the map's parse of settlements_1948.yaml).</summary>
    static IReadOnlyDictionary<string, string> NamesHe() =>
        Statecraft.Game.Map.SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", "settlements_1948.yaml"))).ToDictionary(s => s.Id, s => s.NameHe);

    sealed class Rig : System.IDisposable
    {
        public readonly VisualElement Root; public readonly SimBridge Bridge; public readonly ScreenNav Nav;
        public readonly EventCardPresenter Card; public readonly CaptureDialogPresenter Dialog; public readonly UiText Text;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt; readonly List<System.IDisposable> owned = new List<System.IDisposable>();

        public Rig(GameSession g, UiText? text = null, int w = 1920, int h = 1080,
            System.Func<Content, GameState, string, IReadOnlyList<CaptureOption>>? options = null,
            System.Func<Content, GameState, string, IReadOnlyList<CaptureOption>>? endOptions = null)
        {
            Text = text ?? En;
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("CaptureDialogTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root").RemoveFromClassList("loading");
            Root.Q("root").EnableInClassList(UiLanguage.HebrewClass, Text["ui.lang"] == UiLanguage.Hebrew);   // as GameRoot
            Bridge = new SimBridge(g, Text);
            owned.Add(new HudPresenter(Root, Bridge, Text));
            Nav = new ScreenNav(Root); owned.Add(Nav);
            owned.Add(new InfoPanelPresenter(Root, Bridge, Text, g.Content.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)));
            Card = new EventCardPresenter(Root, Bridge, Text, null); owned.Add(Card);   // as GameRoot: the card first
            Dialog = new CaptureDialogPresenter(Root, Bridge, Text, Card, NamesHe(), helper: options, endHelper: endOptions); owned.Add(Dialog);
        }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }

        public VisualElement Element => Root.Q("capture-card");
        public List<Button> Options => Root.Q("capture-options").Query<Button>().ToList();
        public Button Hide => Root.Q<Button>("capture-hide");
        public Button Waiting => Root.Q<Button>("capture-reopen");
        public Focusable? Focused => Root.panel.focusController.focusedElement;
        public bool Shows(string name) => Root.Q(name).resolvedStyle.display == DisplayStyle.Flex;
        public VisualElement Hud => Root.Q("root");
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static void Press(Button b)
    {
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
    }

    static void Cancel(VisualElement on)
    {
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = on; on.SendEvent(e); }
    }

    static void Tab(VisualElement from)
    {
        using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = from; from.SendEvent(e); }
    }

    /// <summary>A stand-in for an options helper that offers every outcome with a preview, in <paramref name="order"/>, and
    /// refuses <paramref name="refused"/> as needs_unit: the dialog must render whatever the helper returns.</summary>
    static System.Func<Content, GameState, string, IReadOnlyList<CaptureOption>> Helper(string[] order, params string[] refused) =>
        Refusing(order, CaptureRefusal.NeedsUnit, refused);

    /// <summary>As <see cref="Helper"/>, refusing <paramref name="refused"/> with the refusal <paramref name="id"/> and
    /// <paramref name="args"/>.</summary>
    static System.Func<Content, GameState, string, IReadOnlyList<CaptureOption>> Refusing(string[] order, string id, string[] refused, params object[] args) =>
        (c, s, place) => order.Select(o => refused.Contains(o) ? new CaptureOption(o, new CaptureRefusal(id, "refused in this test", args), null)
            : new CaptureOption(o, null, Full)).ToList();

    static readonly string[] EndOrder = { SettlementControl.Annex, SettlementControl.WithdrawOutcome };

    /// <summary>A preview that changes every quantity (seven lines): the most a preview can show.</summary>
    static readonly CapturePreview Full = new CapturePreview(-1, -2, 0.5, -3, -4, -0.05, -0.1, -5);

    /// <summary>The three options with previews of the size the rules give them (docs/gdd/settlement-control.md): Raid changes
    /// nothing at once, Occupy costs Money, Deport costs Social cohesion and International support. Test values.</summary>
    static IReadOnlyList<CaptureOption> Plausible(Content c, GameState s, string id) => new List<CaptureOption>
    {
        new CaptureOption("raid", null, new CapturePreview(0, 0, 0, 0, 0, 0, 0, 0)),
        new CaptureOption("occupy", null, new CapturePreview(0, -0.4, 0, 0, 0, 0, 0, 0)),
        new CaptureOption("deport", null, new CapturePreview(0, 0, 0, 0, 0, -0.08, -0.12, 0)),
    };

    // ---- The dialog opens for a record awaiting the choice ----

    [UnityTest]
    public IEnumerator TheDialogOpensForACaptureAwaitingTheChoice()
    {
        using (var r = new Rig(Awaiting(C, "lod")))
        {
            yield return Frames(4);
            Assert.AreEqual("lod", r.Dialog.Shown);
            Assert.IsTrue(r.Shows("capture-card"), "the dialog shows");
            Assert.AreEqual("Lydda", r.Root.Q<Label>("capture-title").text, "the place's name (settlements_1948.yaml)");
            Assert.IsFalse(r.Root.Q("capture-card").Query<Label>().ToList().Any(l => l.text.Contains(Operation)),
                "the target row's operation line is not shown (owner, 22 October 2026, #1317)");
            Assert.AreEqual(En.Format("capture.intro", "Lydda"), r.Root.Q<Label>("capture-intro").text);
            Assert.IsTrue(r.Hud.ClassListContains(CaptureDialogPresenter.OpenClass) && r.Hud.ClassListContains(EventCardPresenter.ModalClass),
                "the root says the dialog is open, and it is modal like the decision card");
            Assert.IsTrue(r.Shows("event-scrim"), "the backdrop shows");
            Assert.IsFalse(r.Root.Q("info-panel").enabledSelf, "the panels under the backdrop are out of reach");
            Assert.IsFalse(r.Shows("capture-reopen"), "no waiting button while it is open");
        }
        using (var r = new Rig(Awaiting(C)))
        {
            yield return Frames(3);
            Assert.IsNull(r.Dialog.Shown);
            Assert.IsFalse(r.Shows("capture-card") || r.Shows("capture-reopen"), "no capture, no dialog");
            Assert.IsFalse(r.Hud.ClassListContains(CaptureDialogPresenter.OpenClass));
            Assert.IsTrue(r.Root.Q("info-panel").enabledSelf);
        }
        // A record in another state (a later outcome) is not awaiting the choice.
        var g = Awaiting(C);
        g.State.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, "occupied", g.State.Date);
        using (var r = new Rig(g))
        {
            yield return Frames(3);
            Assert.IsNull(r.Dialog.Shown, "only awaiting_choice opens the dialog");
            Assert.IsFalse(r.Shows("capture-card"));
        }
    }

    /// <summary>The dialog follows the sim's state: a capture the turn makes opens it after End Turn.</summary>
    [UnityTest]
    public IEnumerator ACaptureFromTheTurnOpensTheDialog()
    {
        using var r = new Rig(Awaiting(C));
        yield return Frames(3);
        Assert.IsNull(r.Dialog.Shown);
        r.Bridge.State.Settlements["ramla"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, r.Bridge.State.Date);
        r.Bridge.Replace(r.Bridge.Session);   // Changed, as after a turn
        yield return Frames(3);
        Assert.AreEqual("ramla", r.Dialog.Shown);
        Assert.IsTrue(r.Shows("capture-card"));
        Assert.AreEqual("Ramle", r.Root.Q<Label>("capture-title").text);
    }

    // ---- The options: exactly the helper's, in its order ----

    /// <summary>Plan PR 10, CaptureDialogTests.TheOrderIsTheSims: the buttons are the options the real helper lists, in its
    /// order, named in the GDD's words, those it refuses disabled; Deport, out of play (mvp.deport_in_play off, R-CTRL-007),
    /// is left out until the owner's review. With no unit on Lydda, Occupy is refused (R-CTRL-011).</summary>
    [UnityTest]
    public IEnumerator TheOrderIsTheSims()
    {
        using var r = new Rig(Awaiting(C, "lod"));
        yield return Frames(4);
        var helper = SettlementControl.CaptureOptions(r.Bridge.Content, r.Bridge.State, "lod");
        Assert.AreEqual(CaptureRefusal.NotInPlay, helper.Single(o => o.Outcome == SettlementControl.Deport).Refused?.Id, "precondition: Deport is out of play");
        var listed = helper.Where(o => o.Refused?.Id != CaptureRefusal.NotInPlay).Select(o => o.Outcome).ToList();
        CollectionAssert.AreEqual(new[] { SettlementControl.Raid, SettlementControl.Occupy }, listed, "precondition: Raid and Occupy");
        CollectionAssert.AreEqual(listed, r.Dialog.Listed.Select(o => o.Outcome).ToList(), "the dialog's options are the helper's, less the one out of play");
        CollectionAssert.AreEqual(listed.Select(o => "capture-option-" + o).ToList(), r.Options.Select(b => b.name).ToList(), "one button each, in the helper's order");
        CollectionAssert.AreEqual(listed.Select(o => En["capture.option." + o]).ToList(), r.Options.Select(b => b.text).ToList());
        foreach (var o in listed)
            Assert.AreEqual(En["capture.about." + o], r.Root.Q<Label>("capture-about-" + o).text, $"{o}: its plain description");
        CollectionAssert.AreEqual(helper.Where(o => listed.Contains(o.Outcome)).Select(o => o.Refused == null).ToList(), r.Options.Select(b => b.enabledSelf).ToList(),
            "an option the helper refuses is disabled");
        Assert.IsNull(r.Root.Q("capture-option-deport"), "Deport out of play is not shown");
        Assert.IsFalse(r.Shows("capture-none"));
    }

    /// <summary>The dialog renders whatever the helper returns, in the helper's order whatever it is: an offered outcome as a
    /// button with its preview, a refused one as a disabled button with its reason in the UI's language and no preview
    /// (owner, 24 October 2026 on #799, "button explains"), never one refused as out of play.</summary>
    [UnityTest]
    public IEnumerator ItShowsWhateverTheHelperListsInItsOrder()
    {
        foreach (var (order, refused) in new[]
        {
            (new[] { "raid", "occupy", "deport" }, new string[0]),
            (new[] { "deport", "raid", "occupy" }, new string[0]),          // not a UI order: the helper's
            (new[] { "raid", "occupy", "deport" }, new[] { "occupy", "deport" }),
            (new[] { "raid", "occupy", "deport" }, new[] { "raid" }),
        })
        {
            using var r = new Rig(Awaiting(C, "lod"), options: Helper(order, refused));
            yield return Frames(3);
            var where = $"[{string.Join(",", order)}] refusing [{string.Join(",", refused)}]";
            CollectionAssert.AreEqual(order.Select(o => "capture-option-" + o).ToList(), r.Options.Select(b => b.name).ToList(), where);
            foreach (var o in order)
            {
                var b = r.Root.Q<Button>("capture-option-" + o);
                Assert.AreEqual(!refused.Contains(o), b.enabledSelf, $"{where}: {o} is enabled only when offered");
                Assert.AreEqual(refused.Contains(o), r.Root.Q("capture-refusal-" + o) != null, $"{where}: {o} explains a refusal");
                Assert.AreEqual(!refused.Contains(o), r.Root.Q("capture-preview-" + o) != null, $"{where}: {o} has the sim's preview only when offered");
                if (refused.Contains(o)) Assert.AreEqual(En.Format("capture.refusal.needs_unit", "Lydda"), r.Root.Q<Label>("capture-refusal-" + o).text);
            }
            foreach (var o in refused) Press(r.Root.Q<Button>("capture-option-" + o));
            yield return Frames(2);
            Assert.IsEmpty(r.Bridge.Queued, $"{where}: a refused option cannot be chosen");
        }
        using (var r = new Rig(Awaiting(C, "lod"), options: Refusing(new[] { "raid", "occupy", "deport" }, CaptureRefusal.NotInPlay, new[] { "deport" })))
        {
            yield return Frames(3);
            CollectionAssert.AreEqual(new[] { "capture-option-raid", "capture-option-occupy" }, r.Options.Select(b => b.name).ToList(), "an option out of play is not shown");
        }
    }

    /// <summary>R-CTRL-018 (owner, 24 October 2026 on #799, "Hide it, button explains"): for a town whose residents fled at
    /// capture, Deport stays third, disabled, and says why in the UI's language; the other refusals likewise, with the
    /// sim's values (Attention: what it needs, what is left).</summary>
    [UnityTest]
    public IEnumerator ARefusedOptionSaysWhyInTheUisLanguage()
    {
        foreach (var text in new[] { En, He })
        {
            var name = text["ui.lang"] == UiLanguage.Hebrew ? "לוד" : "Lydda";
            using (var r = new Rig(Awaiting(C, "lod"), text, options: Refusing(new[] { "raid", "occupy", "deport" }, CaptureRefusal.NoOneToDeport, new[] { "deport" })))
            {
                yield return Frames(3);
                CollectionAssert.AreEqual(new[] { "capture-option-raid", "capture-option-occupy", "capture-option-deport" }, r.Options.Select(b => b.name).ToList());
                Assert.IsFalse(r.Root.Q<Button>("capture-option-deport").enabledSelf);
                Assert.AreEqual(text.Format("capture.refusal.no_one_to_deport", name), r.Root.Q<Label>("capture-refusal-deport").text);
            }
            using (var r = new Rig(Awaiting(C, "lod"), text, options: Refusing(new[] { "raid", "occupy", "deport" }, CaptureRefusal.Attention, new[] { "raid", "occupy", "deport" }, 2.0, 0.5)))
            {
                yield return Frames(3);
                Assert.AreEqual(text.Format("capture.refusal.attention", name, 2.0, 0.5), r.Root.Q<Label>("capture-refusal-raid").text);
                StringAssert.Contains("2.0", r.Root.Q<Label>("capture-refusal-raid").text, "the sim's numbers");
                StringAssert.Contains("0.5", r.Root.Q<Label>("capture-refusal-raid").text);
            }
        }
    }

    /// <summary>No option the sim offers can be chosen (a Raid with no free hex): every option is disabled, the dialog says
    /// the decision waits, and Hide is still there.</summary>
    [UnityTest]
    public IEnumerator WithNothingOfferedTheDialogSaysTheDecisionWaits()
    {
        using var r = new Rig(Awaiting(C, "lod"), options: Helper(new[] { "raid", "occupy", "deport" }, "raid", "occupy", "deport"));
        yield return Frames(3);
        Assert.AreEqual(3, r.Options.Count);
        Assert.IsTrue(r.Options.All(b => !b.enabledSelf), "every option refused");
        Assert.IsTrue(r.Shows("capture-none"));
        Assert.AreEqual(En["capture.none"], r.Root.Q<Label>("capture-none").text);
        Assert.IsTrue(r.Shows("capture-hide"), "Hide is still there");
    }

    // ---- Nothing is pre-selected ----

    /// <summary>Plan PR 10, CaptureDialogTests.NothingIsPreselected: on opening the focus is on the dialog itself, no button
    /// has it, and every option looks the same (no default style); Tab then reaches the options in order, then Hide.</summary>
    [UnityTest]
    public IEnumerator NothingIsPreselected()
    {
        using var r = new Rig(Awaiting(C, "lod"), options: Plausible);
        yield return Frames(4);
        Assert.AreSame(r.Element, r.Focused, "the dialog itself has the focus, not an option");
        var classes = r.Options.Select(b => string.Join(" ", b.GetClasses().OrderBy(x => x))).Distinct().ToList();
        Assert.AreEqual(1, classes.Count, "every option has the same classes: " + string.Join(" | ", classes));
        Assert.IsTrue(r.Options.All(b => b.enabledSelf), "every option can be chosen");
        // Tab: the options in the helper's order, then Hide.
        VisualElement at = r.Element;
        foreach (var expected in r.Options.Cast<VisualElement>().Append(r.Hide))
        {
            Tab(at);
            yield return Frames(1);
            Assert.AreSame(expected, r.Focused, $"Tab from {at.name} reaches {expected.name}");
            at = expected;
        }
        Assert.IsEmpty(r.Bridge.Queued, "moving the focus chooses nothing");
    }

    // ---- The preview is the sim's ----

    /// <summary>Plan PR 10, CaptureDialogTests.ThePreviewIsTheSims: under each option, the helper's own preview, formatted as
    /// the top bar formats each quantity; with resolve_capture priced at 1 the sim's Attention change shows.</summary>
    [UnityTest]
    public IEnumerator ThePreviewIsTheSims()
    {
        var c = Targets(price: 1);
        using var r = new Rig(Awaiting(c, "lod"));
        yield return Frames(4);
        var helper = SettlementControl.CaptureOptions(r.Bridge.Content, r.Bridge.State, "lod").Where(o => o.Refusal == null).ToList();
        Assert.IsNotEmpty(helper);
        foreach (var o in helper)
        {
            var shown = r.Root.Q<Label>("capture-preview-" + o.Outcome).text;
            Assert.AreEqual(CaptureDialogPresenter.PreviewText(r.Bridge.Content, r.Bridge.State, o.Preview!, En), shown, $"{o.Outcome}: the helper's preview");
            Assert.Less(o.Preview!.Attention, 0, "precondition: the sim charges Attention for the order");
            StringAssert.Contains(En.Format("report.line", En["capture.caption.attention"], En.Format("capture.delta_attention", o.Preview.Attention)), shown,
                $"{o.Outcome}: the sim's own Attention change");
            Assert.AreEqual(-r.Bridge.Price(new ResolveCapture("lod", o.Outcome)), o.Preview.Attention, 1e-9, "the order's price at this turn's prices");
        }
        using var free = new Rig(Awaiting(C, "lod"));
        yield return Frames(3);
        Assert.AreEqual(En["capture.changes_none"], free.Root.Q<Label>("capture-preview-raid").text, "Raid changes nothing at once (R-CTRL-010)");
    }

    /// <summary>Each quantity of the preview as the top bar shows it, in CapturePreview's order, a change that rounds to
    /// nothing left out: Money is the net change (the Treasury less the debt, PlayerText.MoneyChange), the 0-1 indicators are
    /// the difference of the two shown 0-100 values (PlayerText.PercentChangeShown).</summary>
    [Test]
    public void ThePreviewTextFormatsEachQuantityAsTheTopBarDoes()
    {
        var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;
        var morale = s.Indicators["morale"]; var standing = s.Indicators["intl_standing"];
        var p = new CapturePreview(-1, -2, 0.5, -3, -4, -0.05, -0.1, -5);
        var expected = string.Join("\n", new[]
        {
            En["capture.changes"],
            En.Format("report.line", En["capture.caption.attention"], "-1.0"),
            En.Format("report.line", En["hud.caption.treasury"], PlayerText.MoneyChange(C, "treasury", -2.5)),
            En.Format("report.line", En["capture.caption.food"], "-3"),
            En.Format("report.line", En["capture.caption.arms"], "-4"),
            En.Format("report.line", En["hud.caption.cohesion"], PlayerText.PercentChangeShown(morale, morale - 0.05)),
            En.Format("report.line", En["hud.caption.standing"], PlayerText.PercentChangeShown(standing, standing - 0.1)),
            En.Format("report.line", En["capture.caption.score"], "-5"),
        });
        Assert.AreEqual(expected, CaptureDialogPresenter.PreviewText(C, s, p, En));
        StringAssert.Contains("Score: -5", expected);
        Assert.AreEqual(En["capture.changes_none"], CaptureDialogPresenter.PreviewText(C, s, new CapturePreview(0, 0.01, 0, 0, 0, 0.001, 0, 0), En),
            "changes too small to show are left out, as on the top bar");
        var he = CaptureDialogPresenter.PreviewText(C, s, p, He);
        StringAssert.StartsWith(He["capture.changes"], he);
        StringAssert.Contains(He.Format("report.line", He["capture.caption.attention"], "-1.0"), he, "the Hebrew line marks the number left to right");
    }

    // ---- Choosing issues ResolveCapture ----

    /// <summary>A choice is an order: it queues ResolveCapture(place, outcome) through SimBridge, the dialog goes on to the
    /// next capture in the sim's (ordinal) order, and End Turn carries it out.</summary>
    [UnityTest]
    public IEnumerator ChoosingIssuesResolveCapture()
    {
        using var r = new Rig(Awaiting(C, "ramla", "lod"));
        yield return Frames(4);
        Assert.AreEqual("lod", r.Dialog.Shown, "the first in the sim's order");
        Assert.AreEqual(En.Format("event.more", 1), r.Root.Q<Label>("capture-count").text);
        Press(r.Root.Q<Button>("capture-option-raid"));
        yield return Frames(3);
        var queued = r.Bridge.Queued.OfType<ResolveCapture>().ToList();
        Assert.AreEqual(1, queued.Count);
        Assert.AreEqual(("lod", SettlementControl.Raid), (queued[0].Settlement, queued[0].Outcome));
        Assert.AreEqual("ramla", r.Dialog.Shown, "the next capture");
        Press(r.Root.Q<Button>("capture-option-raid"));
        yield return Frames(3);
        CollectionAssert.AreEqual(new[] { "lod", "ramla" }, r.Bridge.Queued.OfType<ResolveCapture>().Select(x => x.Settlement).ToList());
        Assert.IsNull(r.Dialog.Shown, "every capture answered");
        Assert.IsFalse(r.Shows("capture-card"));
        Assert.AreSame(r.Root.Q<Button>("end-turn"), r.Focused, "answered from the keyboard, the focus goes to End turn");
        var result = r.Bridge.EndTurn();
        Assert.IsEmpty(result.Rejected.Where(x => x.Command is ResolveCapture), "the sim takes both");
        Assert.IsFalse(r.Bridge.State.Settlements.ContainsKey("lod") || r.Bridge.State.Settlements.ContainsKey("ramla"), "Raid ended both records");
    }

    // ---- Escape does not choose ----

    /// <summary>Escape on the open dialog is Hide, as on the decision card: nothing is queued, "Decision waiting: place" takes
    /// the focus and brings the dialog back; Escape on a hidden dialog, or under a screen, does nothing.</summary>
    [UnityTest]
    public IEnumerator EscapeHidesAndDoesNotChoose()
    {
        using var r = new Rig(Awaiting(C, "lod"));
        yield return Frames(4);
        Cancel(r.Element);
        yield return Frames(3);
        Assert.IsTrue(r.Dialog.Collapsed, "Escape hides the dialog");
        Assert.IsEmpty(r.Bridge.Queued, "and chooses nothing");
        Assert.IsFalse(r.Shows("capture-card"));
        Assert.IsTrue(r.Shows("capture-reopen"));
        Assert.AreEqual(En.Format("capture.reopen", "Lydda"), r.Waiting.text);
        Assert.AreSame(r.Waiting, r.Focused, "from the keyboard, the waiting button has the focus");
        Assert.IsFalse(r.Hud.ClassListContains(CaptureDialogPresenter.OpenClass) || r.Hud.ClassListContains(EventCardPresenter.ModalClass), "the map is free");
        Assert.IsFalse(r.Dialog.Escape(), "Escape on a hidden dialog does nothing");
        Press(r.Waiting);
        yield return Frames(3);
        Assert.IsFalse(r.Dialog.Collapsed);
        Assert.AreSame(r.Element, r.Focused, "opened again, the dialog has the focus");
        // Escape from an option: still Hide, never the option.
        Cancel(r.Options[0]);
        yield return Frames(2);
        Assert.IsTrue(r.Dialog.Collapsed);
        Assert.IsEmpty(r.Bridge.Queued);
        Press(r.Waiting);
        yield return Frames(2);
        r.Nav.Open("why");
        yield return Frames(2);
        Assert.IsFalse(r.Dialog.Escape(), "under a screen Escape is the screen's");
        Assert.IsFalse(r.Dialog.Collapsed);
        r.Nav.Close();
        yield return Frames(3);
        Assert.AreSame(r.Element, r.Focused, "closing the screen gives the open dialog the focus");
        Assert.IsEmpty(r.Bridge.Queued);
    }

    // ---- The decision card goes first ----

    /// <summary>One window at a time in the decision layer: while a decision card is open the dialog waits, hidden with no
    /// button of its own; hiding or answering the card brings it; reopening the card puts it back behind.</summary>
    [UnityTest]
    public IEnumerator TheDecisionCardGoesFirst()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);   // a new game opens on a decision card
        Assert.IsNotEmpty(g.State.PendingEvents, "precondition: an event is pending");
        g.State.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, g.State.Date);
        using var r = new Rig(g);
        yield return Frames(4);
        Assert.IsNotNull(r.Card.Shown, "precondition: a decision card is open");
        Assert.AreEqual("lod", r.Dialog.Shown, "the capture is pending");
        Assert.IsFalse(r.Shows("capture-card") || r.Shows("capture-reopen"), "but waits behind the card");
        r.Card.Collapse();
        yield return Frames(3);
        Assert.IsTrue(r.Shows("capture-card"), "the card hidden, the dialog opens");
        Assert.IsTrue(r.Shows("event-reopen") && !r.Shows("capture-reopen"), "the card's Decision waiting stays, alone in its place");
        r.Card.Reopen();
        yield return Frames(3);
        Assert.IsTrue(r.Shows("event-card") && !r.Shows("capture-card"), "the card again, the dialog behind it");
        Press(r.Root.Q("event-options").Q<Button>());
        yield return Frames(3);
        Assert.AreEqual("degania_defence", r.Card.Shown?.Id, "One Army answered, the turn's second card (R-EVT-006)");
        Assert.IsFalse(r.Shows("capture-card"), "and the dialog still waits behind a card");
        Press(r.Root.Q("event-options").Q<Button>());
        yield return Frames(3);
        Assert.IsNull(r.Card.Shown, "precondition: the opening turn's two cards are answered");
        Assert.IsTrue(r.Shows("capture-card"), "then the dialog");
        Assert.AreSame(r.Element, r.Focused, "answered from the keyboard, the dialog that opens takes the focus (not End turn)");
    }

    // ---- No sound and no video ----

    sealed class Speaker : GameAudio.IOutput
    {
        public readonly List<string> Played = new List<string>();
        public void Play(AudioClip clip, float volume) => Played.Add(clip.name);
        public void Dispose() { }
    }

    /// <summary>Plan PR 10, CaptureDialogTests.NoSoundAndNoVideo (impact plan section 4.1 item 6): opening the dialog and
    /// choosing play no clip and no sound (GameAudio, with every cue's clip present, plays nothing), and the dialog holds no
    /// picture, no video player and no audio of its own.</summary>
    [UnityTest]
    public IEnumerator NoSoundAndNoVideo()
    {
        var made = new List<AudioClip>();
        var speaker = new Speaker();
        using var r = new Rig(Awaiting(C, "lod"));
        var audio = new GameAudio(r.Bridge, r.Card, path => { var a = AudioClip.Create(path, 441, 1, 44100, false); made.Add(a); return a; }, speaker);
        try
        {
            r.Dialog.Reopen();   // opened again with the speaker listening
            yield return Frames(4);
            Assert.IsTrue(r.Shows("capture-card"));
            Press(r.Root.Q<Button>("capture-option-raid"));
            yield return Frames(3);
            Assert.IsNotEmpty(r.Bridge.Queued, "precondition: chosen");
            Assert.IsEmpty(speaker.Played, "no sound on opening or choosing");
            foreach (var e in r.Element.Query<VisualElement>().ToList().Prepend(r.Element).Where(e => e.GetFirstAncestorOfType<Scroller>() == null && !(e is Scroller)))
                Assert.IsNull(e.resolvedStyle.backgroundImage.texture, $"{e.name}: no picture in the dialog");
            var kinds = typeof(CaptureDialogPresenter).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Select(f => f.FieldType).ToList();
            foreach (var banned in new[] { typeof(CardVideo), typeof(GameAudio), typeof(AudioSource), typeof(AudioClip) })
                Assert.IsFalse(kinds.Contains(banned), $"the dialog holds no {banned.Name}");
        }
        finally
        {
            audio.Dispose();
            foreach (var a in made) Object.DestroyImmediate(a);
        }
    }

    // ---- The strings ----

    /// <summary>Every outcome the helper lists, offered or not (Raid, Occupy, Deport), has its name and its description in
    /// English and Hebrew, so a newly built outcome shows in both at once; the Hebrew ones are Hebrew.</summary>
    [Test]
    public void EveryOutcomeHasItsStringsInBothLanguages()
    {
        var g = Awaiting(C, "lod");
        var outcomes = SettlementControl.CaptureOptions(C, g.State, "lod").Select(o => o.Outcome).ToList();
        CollectionAssert.AreEqual(new[] { SettlementControl.Raid, SettlementControl.Occupy, SettlementControl.Deport }, outcomes, "precondition: the helper lists the three");
        var keys = outcomes.SelectMany(o => new[] { "capture.option." + o, "capture.about." + o })
            .Concat(new[] { "capture.intro", "capture.changes", "capture.changes_none", "capture.caption.attention", "capture.caption.food",
                "capture.caption.arms", "capture.caption.score", "capture.delta_attention", "capture.delta_count", "capture.none", "capture.reopen" });
        foreach (var key in keys)
        {
            Assert.IsTrue(En.Has(key), $"strings.en.yaml: {key}");
            Assert.IsTrue(He.Has(key), $"strings.he.yaml: {key}");
            if (!key.StartsWith("capture.delta_")) Assert.IsTrue(UiDirection.HasRightToLeft(He[key]), $"strings.he.yaml: {key} is Hebrew: {He[key]}");
        }
    }

    /// <summary>In Hebrew the dialog names the place by its name_he and shows the Hebrew strings.</summary>
    [UnityTest]
    public IEnumerator InHebrewTheDialogIsHebrew()
    {
        using var r = new Rig(Awaiting(C, "lod"), He);
        using var direction = new UiDirection(r.Root, r.Bridge);   // as GameRoot in Hebrew
        yield return Frames(4);
        Assert.AreEqual("לוד", r.Root.Q<Label>("capture-title").text);
        Assert.AreEqual(He.Format("capture.intro", "לוד"), r.Root.Q<Label>("capture-intro").text);
        Assert.AreEqual(He["capture.option.raid"], r.Root.Q<Button>("capture-option-raid").text);
        Assert.AreEqual(He["capture.about.raid"], r.Root.Q<Label>("capture-about-raid").text);
        Assert.AreEqual(He["capture.changes_none"], r.Root.Q<Label>("capture-preview-raid").text);
        Assert.AreEqual(He["event.hide"], r.Hide.text);
        foreach (var name in new[] { "capture-title", "capture-intro", "capture-option-raid", "capture-about-raid", "capture-preview-raid" })
            Assert.AreEqual(LanguageDirection.RTL, r.Root.Q<TextElement>(name).languageDirection, $"{name} reads right to left");
    }

    // ---- The end of an occupation (R-CTRL-014, R-CTRL-015) ----

    /// <summary>A record awaiting the end of its occupation opens the dialog for it: its own intro, Annex then Withdraw in
    /// the GDD's words, a stayed or fled place's Annex description, the reopen line shared with the capture.</summary>
    [UnityTest]
    public IEnumerator TheDialogOpensForAnOccupationAwaitingItsEnd()
    {
        foreach (var fled in new[] { false, true })
        {
            using var r = new Rig(Ending(C, true, fled, "lod"), endOptions: Helper(EndOrder));
            yield return Frames(4);
            Assert.AreEqual("lod", r.Dialog.Shown);
            Assert.IsTrue(r.Dialog.Ending, "the end of the occupation, not a capture");
            Assert.IsTrue(r.Shows("capture-card"));
            Assert.AreEqual("Lydda", r.Root.Q<Label>("capture-title").text);
            Assert.AreEqual(En.Format("capture.end.intro", "Lydda"), r.Root.Q<Label>("capture-intro").text);
            CollectionAssert.AreEqual(new[] { En["capture.end.option.annex"], En["capture.end.option.withdraw"] }, r.Options.Select(b => b.text).ToList());
            Assert.AreEqual(En[fled ? "capture.end.about.annex_fled" : "capture.end.about.annex_stayed"], r.Root.Q<Label>("capture-about-annex").text,
                "Annex says what it adds: nothing for a fled place (R-CTRL-016)");
            Assert.AreEqual(En["capture.end.about.withdraw"], r.Root.Q<Label>("capture-about-withdraw").text);
            Cancel(r.Element);
            yield return Frames(2);
            Assert.AreEqual(En.Format("capture.reopen", "Lydda"), r.Waiting.text);
        }
    }

    /// <summary>Plan PR 10, TheEndOrderIsTheSims (R-CTRL-015): the buttons are the real EndOptions, Annex then Withdraw, no
    /// puppet regime and nothing the UI made up; enabled with the garrison there.</summary>
    [UnityTest]
    public IEnumerator TheEndOrderIsTheSims()
    {
        using var r = new Rig(Ending(C, true, false, "lod"));
        yield return Frames(4);
        var helper = SettlementControl.EndOptions(r.Bridge.Content, r.Bridge.State, "lod");
        CollectionAssert.AreEqual(EndOrder, helper.Select(o => o.Outcome).ToList(), "precondition: the sim's order");
        Assert.IsTrue(helper.All(o => o.Refused == null), "precondition: the garrison is there: " + string.Join("; ", helper.Select(o => o.Refusal)));
        CollectionAssert.AreEqual(EndOrder.Select(o => "capture-option-" + o).ToList(), r.Options.Select(b => b.name).ToList());
        Assert.IsTrue(r.Options.All(b => b.enabledSelf));
    }

    /// <summary>Plan PR 10, EndNothingIsPreselected: the dialog itself has the focus, the options look alike, Tab reaches
    /// Annex, Withdraw, then Hide.</summary>
    [UnityTest]
    public IEnumerator EndNothingIsPreselected()
    {
        using var r = new Rig(Ending(C, true, false, "lod"), endOptions: Helper(EndOrder));
        yield return Frames(4);
        Assert.AreSame(r.Element, r.Focused, "the dialog itself has the focus, not an option");
        Assert.AreEqual(1, r.Options.Select(b => string.Join(" ", b.GetClasses().OrderBy(x => x))).Distinct().Count(), "no default style");
        VisualElement at = r.Element;
        foreach (var expected in r.Options.Cast<VisualElement>().Append(r.Hide))
        {
            Tab(at);
            yield return Frames(1);
            Assert.AreSame(expected, r.Focused, $"Tab from {at.name} reaches {expected.name}");
            at = expected;
        }
        Assert.IsEmpty(r.Bridge.Queued);
    }

    /// <summary>Plan PR 10, TheEndPreviewIsTheSims: under each option, EndOptions' own preview, headed as the resources only
    /// (the people, homes and jobs Annex adds are in its description, not in numbers the UI makes); priced at 1, the sim's
    /// Attention change shows.</summary>
    [UnityTest]
    public IEnumerator TheEndPreviewIsTheSims()
    {
        var c = Targets(endPrice: 1);
        using var r = new Rig(Ending(c, true, false, "lod"));
        yield return Frames(4);
        foreach (var o in SettlementControl.EndOptions(r.Bridge.Content, r.Bridge.State, "lod"))
        {
            Assert.IsNull(o.Refused, "precondition: " + o.Refusal);
            var shown = r.Root.Q<Label>("capture-preview-" + o.Outcome).text;
            Assert.AreEqual(CaptureDialogPresenter.PreviewText(r.Bridge.Content, r.Bridge.State, o.Preview!, En, ending: true), shown);
            StringAssert.StartsWith(En["capture.end.changes"], shown);
            StringAssert.Contains(En.Format("report.line", En["capture.caption.attention"], En.Format("capture.delta_attention", o.Preview!.Attention)), shown);
            Assert.AreEqual(-r.Bridge.Price(new EndOccupation("lod", o.Outcome)), o.Preview.Attention, 1e-9);
        }
        using var free = new Rig(Ending(C, true, false, "lod"));
        yield return Frames(3);
        Assert.AreEqual(En["capture.end.changes_none"], free.Root.Q<Label>("capture-preview-annex").text);
    }

    /// <summary>Plan PR 10, ChoosingIssuesEndOccupation (R-CTRL-014): a choice queues EndOccupation(place, outcome), changes
    /// nothing until End turn, goes on to the next place; End turn carries it out.</summary>
    [UnityTest]
    public IEnumerator ChoosingIssuesEndOccupation()
    {
        using var r = new Rig(Ending(C, true, false, "lod", "ramla"));
        yield return Frames(4);
        Assert.AreEqual("lod", r.Dialog.Shown);
        Press(r.Root.Q<Button>("capture-option-annex"));
        yield return Frames(3);
        var queued = r.Bridge.Queued.OfType<EndOccupation>().Single();
        Assert.AreEqual(("lod", SettlementControl.Annex), (queued.Settlement, queued.Outcome));
        Assert.AreEqual(SettlementControl.AwaitingEnd, r.Bridge.State.Settlements["lod"].State, "nothing happens before End turn");
        Assert.AreEqual("ramla", r.Dialog.Shown, "the next place");
        Assert.IsTrue(r.Dialog.Ending);
        Press(r.Root.Q<Button>("capture-option-withdraw"));
        yield return Frames(3);
        Assert.IsNull(r.Dialog.Shown);
        Assert.AreSame(r.Root.Q<Button>("end-turn"), r.Focused, "answered from the keyboard, the focus goes to End turn");
        var result = r.Bridge.EndTurn();
        Assert.IsEmpty(result.Rejected.Where(x => x.Command is EndOccupation), string.Join("; ", result.Rejected.Select(x => x.Reason)));
        Assert.AreEqual(SettlementControl.Annexed, r.Bridge.State.Settlements["lod"].State, "End turn annexed Lydda (R-CTRL-016)");
        Assert.IsFalse(r.Bridge.State.Settlements.ContainsKey("ramla"), "and withdrew from Ramle (R-CTRL-010)");
    }

    /// <summary>Plan PR 10, AQueuedEndDecisionIsNotShownAgain: a place with an EndOccupation queued has had its choice; a new
    /// answer replaces it in the one slot (SimBridge.Key).</summary>
    [UnityTest]
    public IEnumerator AQueuedEndDecisionIsNotShownAgain()
    {
        using var r = new Rig(Ending(C, true, false, "lod"));
        yield return Frames(3);
        Assert.IsNull(r.Bridge.Queue(new EndOccupation("lod", SettlementControl.Annex)));
        yield return Frames(3);
        Assert.IsNull(r.Dialog.Shown, "answered");
        Assert.IsNull(r.Bridge.Queue(new EndOccupation("lod", SettlementControl.WithdrawOutcome)));
        Assert.AreEqual(SettlementControl.WithdrawOutcome, r.Bridge.Queued.OfType<EndOccupation>().Single().Outcome, "one slot, the later answer");
    }

    /// <summary>Plan PR 10, EndEscapeHidesAndDoesNotChoose: Escape hides the end dialog, queues nothing, and Decision waiting
    /// brings it back with the focus on it; the occupation keeps waiting (R-CTRL-014).</summary>
    [UnityTest]
    public IEnumerator EndEscapeHidesAndDoesNotChoose()
    {
        using var r = new Rig(Ending(C, true, false, "lod"), endOptions: Helper(EndOrder));
        yield return Frames(4);
        Cancel(r.Element);
        yield return Frames(3);
        Assert.IsTrue(r.Dialog.Collapsed);
        Assert.IsEmpty(r.Bridge.Queued);
        Assert.AreSame(r.Waiting, r.Focused);
        Press(r.Waiting);
        yield return Frames(3);
        Assert.IsFalse(r.Dialog.Collapsed);
        Assert.AreSame(r.Element, r.Focused);
        Assert.AreEqual(SettlementControl.AwaitingEnd, r.Bridge.State.Settlements["lod"].State);
    }

    /// <summary>The decision card goes before the end dialog as before the capture dialog.</summary>
    [UnityTest]
    public IEnumerator TheDecisionCardGoesBeforeTheEndDialog()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        Assert.IsNotEmpty(g.State.PendingEvents, "precondition: an event is pending");
        g.State.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, g.State.Date);
        using var r = new Rig(g, endOptions: Helper(EndOrder));
        yield return Frames(4);
        Assert.AreEqual("lod", r.Dialog.Shown);
        Assert.IsFalse(r.Shows("capture-card") || r.Shows("capture-reopen"), "waits behind the card");
        r.Card.Collapse();
        yield return Frames(3);
        Assert.IsTrue(r.Shows("capture-card"));
        Assert.IsTrue(r.Dialog.Ending);
    }

    /// <summary>Owner, 24 October 2026 on #802, "By settlement order": captures and ends of occupation go in one ordinal pass
    /// over the places, each showing the decision it has, whichever kind comes first.</summary>
    [UnityTest]
    public IEnumerator CaptureAndEndGoBySettlementOrder()
    {
        foreach (var lodEnds in new[] { true, false })
        {
            var g = Ending(C, true, false);
            g.State.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, lodEnds ? SettlementControl.AwaitingEnd : SettlementControl.AwaitingChoice, g.State.Date);
            g.State.Settlements["ramla"] = new SettlementControl(Movement.PlayerSide, lodEnds ? SettlementControl.AwaitingChoice : SettlementControl.AwaitingEnd, g.State.Date);
            using var r = new Rig(g, options: Helper(new[] { "raid", "occupy" }), endOptions: Helper(EndOrder));
            yield return Frames(4);
            Assert.AreEqual(("lod", lodEnds), (r.Dialog.Shown, r.Dialog.Ending), "Lydda first (ordinal), with its own decision");
            Assert.AreEqual(En.Format("event.more", 1), r.Root.Q<Label>("capture-count").text, "both kinds count");
            Press(r.Options[0]);
            yield return Frames(3);
            Assert.AreEqual(("ramla", !lodEnds), (r.Dialog.Shown, r.Dialog.Ending), "then Ramle, with its own");
            Assert.AreEqual(1, lodEnds ? r.Bridge.Queued.OfType<EndOccupation>().Count() : r.Bridge.Queued.OfType<ResolveCapture>().Count());
        }
    }

    /// <summary>Plan PR 10, RefusedOptionsStayInOrderAndExplainWhy and NoneEnabledStillWaits (R-CTRL-014): with no Israeli
    /// unit on the place, the real EndOptions refuses both; both stay, disabled, in order, each saying why in the UI's
    /// language, with no preview; the dialog says the decision waits; nothing can be queued.</summary>
    [UnityTest]
    public IEnumerator WithNoGarrisonTheEndWaitsAndSaysWhy()
    {
        foreach (var text in new[] { En, He })
        {
            using var r = new Rig(Ending(C, false, false, "lod"), text);
            yield return Frames(4);
            Assert.IsTrue(SettlementControl.EndOptions(r.Bridge.Content, r.Bridge.State, "lod").All(o => o.Refused?.Id == CaptureRefusal.NeedsUnit), "precondition");
            CollectionAssert.AreEqual(EndOrder.Select(o => "capture-option-" + o).ToList(), r.Options.Select(b => b.name).ToList());
            var name = text["ui.lang"] == UiLanguage.Hebrew ? "לוד" : "Lydda";
            foreach (var o in EndOrder)
            {
                Assert.IsFalse(r.Root.Q<Button>("capture-option-" + o).enabledSelf);
                Assert.AreEqual(text.Format("capture.refusal.needs_unit", name), r.Root.Q<Label>("capture-refusal-" + o).text);
                Assert.IsNull(r.Root.Q("capture-preview-" + o));
                Press(r.Root.Q<Button>("capture-option-" + o));
            }
            yield return Frames(2);
            Assert.IsEmpty(r.Bridge.Queued);
            Assert.IsTrue(r.Shows("capture-none"));
            Assert.AreEqual(text["capture.end.none"], r.Root.Q<Label>("capture-none").text);
            Assert.IsTrue(r.Shows("capture-hide"));
        }
    }

    /// <summary>Plan PR 10, EndNoSoundAndNoVideo: opening the end dialog and choosing play nothing.</summary>
    [UnityTest]
    public IEnumerator EndNoSoundAndNoVideo()
    {
        var made = new List<AudioClip>();
        var speaker = new Speaker();
        using var r = new Rig(Ending(C, true, false, "lod"), endOptions: Helper(EndOrder));
        var audio = new GameAudio(r.Bridge, r.Card, path => { var a = AudioClip.Create(path, 441, 1, 44100, false); made.Add(a); return a; }, speaker);
        try
        {
            r.Dialog.Reopen();
            yield return Frames(4);
            Assert.IsTrue(r.Shows("capture-card") && r.Dialog.Ending);
            Press(r.Root.Q<Button>("capture-option-annex"));
            yield return Frames(3);
            Assert.IsNotEmpty(r.Bridge.Queued);
            Assert.IsEmpty(speaker.Played);
        }
        finally
        {
            audio.Dispose();
            foreach (var a in made) Object.DestroyImmediate(a);
        }
    }

    /// <summary>Plan PR 10, EveryEndOutcomeHasReviewedStringsInBothLanguages: every end outcome's name and description, the end
    /// dialog's lines, and a line for every refusal id the dialog can show (all but not_in_play, which it never shows), in
    /// English and Hebrew; the Hebrew ones are Hebrew. Occupy stays שחרור and the held state מוחזק / תקופת ההחזקה (owner,
    /// #1317 and #803): the end dialog never calls the state שחרור.</summary>
    [Test]
    public void EveryEndOutcomeAndRefusalHasItsStringsInBothLanguages()
    {
        var g = Ending(C, true, false, "lod");
        CollectionAssert.AreEqual(EndOrder, SettlementControl.EndOptions(C, g.State, "lod").Select(o => o.Outcome).ToList(), "precondition: Annex then Withdraw");
        var keys = new[] { "capture.end.intro", "capture.end.option.annex", "capture.end.option.withdraw", "capture.end.about.annex_fled",
                "capture.end.about.annex_stayed", "capture.end.about.withdraw", "capture.end.changes", "capture.end.changes_none", "capture.end.none" }
            .Concat(CaptureRefusal.Ids.Where(id => id != CaptureRefusal.NotInPlay).Select(id => "capture.refusal." + id)).ToList();
        foreach (var key in keys)
        {
            Assert.IsTrue(En.Has(key), $"strings.en.yaml: {key}");
            Assert.IsTrue(He.Has(key), $"strings.he.yaml: {key}");
            Assert.IsTrue(UiDirection.HasRightToLeft(He[key]), $"strings.he.yaml: {key} is Hebrew: {He[key]}");
            StringAssert.DoesNotContain("שחרור", He[key], $"{key}: שחרור is Occupy's name only");
        }
        Assert.IsFalse(En.Has("capture.refusal.not_in_play") || He.Has("capture.refusal.not_in_play"), "an option out of play is never shown, so it has no line");
        Assert.AreEqual("שחרור", He["capture.option.occupy"], "Occupy's name (owner, #1317)");
        StringAssert.Contains("תקופת ההחזקה", He["capture.end.intro"], "the held state's words (owner, #803)");
        Assert.AreEqual(En["capture.refusal.no_one_to_deport"].TrimEnd('.'), "Its residents fled when it was captured, so there is no one to deport",
            "the owner's words for the fled town (R-CTRL-018)");
    }

    /// <summary>Plan PR 10, EndInHebrewIsHebrew: in Hebrew the end dialog names the place by its name_he, shows the Hebrew
    /// strings, and reads right to left.</summary>
    [UnityTest]
    public IEnumerator EndInHebrewIsHebrew()
    {
        using var r = new Rig(Ending(C, true, false, "lod"), He);
        using var direction = new UiDirection(r.Root, r.Bridge);
        yield return Frames(4);
        Assert.AreEqual("לוד", r.Root.Q<Label>("capture-title").text);
        Assert.AreEqual(He.Format("capture.end.intro", "לוד"), r.Root.Q<Label>("capture-intro").text);
        Assert.AreEqual(He["capture.end.option.annex"], r.Root.Q<Button>("capture-option-annex").text);
        Assert.AreEqual(He["capture.end.about.annex_stayed"], r.Root.Q<Label>("capture-about-annex").text);
        Assert.AreEqual(He["capture.end.changes_none"], r.Root.Q<Label>("capture-preview-annex").text);
        foreach (var name in new[] { "capture-title", "capture-intro", "capture-option-annex", "capture-about-annex", "capture-preview-annex", "capture-option-withdraw" })
            Assert.AreEqual(LanguageDirection.RTL, r.Root.Q<TextElement>(name).languageDirection, $"{name} reads right to left");
    }

    // ---- The layout ----

    static bool Inside(Rect inner, Rect outer, float slack = 1f) =>
        inner.xMin >= outer.xMin - slack && inner.yMin >= outer.yMin - slack && inner.xMax <= outer.xMax + slack && inner.yMax <= outer.yMax + slack;

    /// <summary>The window shapes of UiLayoutTests.EveryEventCardFitsTheDecisionLayer.</summary>
    static readonly (int w, int h)[] Windows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (390, 844), (960, 600), (1531, 784), (1568, 752), (2560, 1080), (1920, 900), (1366, 657),
    };

    /// <summary>At every window shape the decision card is checked at, in English and Hebrew, with all three options, and
    /// for the end dialog with a refused Withdraw: the dialog lies in the decision layer below the top bar, Hide inside it,
    /// every option and line no wider than the dialog, each option's text inside its button. With previews of the size the
    /// rules give (Plausible), a window logically at least EventCardPresenter.ScrollFallbackHeight tall shows everything
    /// without scrolling; with every quantity changed for every offered option (Full, seven lines each) and a refused one's
    /// reason, the text and options scroll and Hide stays in view.</summary>
    [UnityTest]
    public IEnumerator ItFitsEveryWindowInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var text in new[] { En, He })
            foreach (var (w, h) in Windows)
            foreach (var (full, ending) in new[] { (false, false), (true, false), (true, true) })
            {
                var at = $"{text["ui.lang"]} {w}x{h}{(full ? " full previews" : "")}{(ending ? " end, Withdraw refused" : "")}";
                using var r = ending
                    ? new Rig(Ending(C, true, true, "lod"), text, w, h, endOptions: Helper(EndOrder, SettlementControl.WithdrawOutcome))
                    : new Rig(Awaiting(C, "lod"), text, w, h, full ? Helper(new[] { "raid", "occupy", "deport" }, "deport") : Plausible);
                yield return Frames(4);
                var card = r.Element.worldBound; var layer = r.Root.Q("event-layer").worldBound; var bar = r.Root.Q("top-bar").worldBound;
                if (!(card.width > 0 && card.height > 0)) { problems.Add($"{at}: the dialog is not shown"); continue; }
                if (!Inside(card, layer)) problems.Add($"{at}: the dialog {card} leaves the decision layer {layer}");
                if (card.yMin < bar.yMax - 0.5f) problems.Add($"{at}: the dialog {card} starts over the top bar {bar}");
                if (!Inside(r.Hide.worldBound, card)) problems.Add($"{at}: Hide {r.Hide.worldBound} is cut off by the dialog {card}");
                foreach (var e in r.Root.Q("capture-options").Query<TextElement>().ToList().Concat(new[] { r.Root.Q<Label>("capture-title"), r.Root.Q<Label>("capture-intro") }))
                {
                    if (e.worldBound.xMin < card.xMin - 1 || e.worldBound.xMax > card.xMax + 1) problems.Add($"{at}: '{e.name}' {e.worldBound} is wider than the dialog {card}");
                    if (!UiLayoutTests.TextFits(e)) problems.Add($"{at}: the text of '{e.name}' does not fit {e.contentRect}");
                }
                var scroll = r.Root.Q<ScrollView>("capture-scroll");
                if (!full && r.Root.panel.visualTree.layout.height >= EventCardPresenter.ScrollFallbackHeight && scroll.contentContainer.layout.height > scroll.contentViewport.layout.height + 1f)
                    problems.Add($"{at}: the dialog scrolls ({scroll.contentContainer.layout.height:0} px in {scroll.contentViewport.layout.height:0} px)");
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
