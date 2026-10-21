#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The decision card in the middle (decision cards, 2026-10-13): while it shows, its backdrop takes the pointer and
/// the panels under it are out of reach (ScreenNav.DisableCovered, as #409 does for screens); Hide collapses it to
/// "Decision waiting", which brings it back; a new event opens it again; a new event puts the focus on the card (no option
/// lit as if recommended; Tab goes on to the options) and an open one never takes it back; any open screen, and the start screen, hide it and take it out of reach. Checked on
/// a one-row HUD (1920x1080) and the narrow two-row HUD (960x1080).</summary>
public class DecisionCardTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    sealed class Rig : System.IDisposable
    {
        public readonly VisualElement Root; public readonly SimBridge Bridge; public readonly ScreenNav Nav; public readonly EventCardPresenter Card;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt; readonly List<System.IDisposable> owned = new List<System.IDisposable>();

        public Rig(int w, int h, string eventId, bool photos = false, UiText? text = null)
        {
            var t = text ?? Text;
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("DecisionCardTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root").RemoveFromClassList("loading");
            Root.Q("root").EnableInClassList(UiLanguage.HebrewClass, t["ui.lang"] == UiLanguage.Hebrew);   // as GameRoot
            Bridge = new SimBridge(Pending(eventId), t);
            owned.Add(new HudPresenter(Root, Bridge, t));
            Nav = new ScreenNav(Root); owned.Add(Nav);
            owned.Add(new InfoPanelPresenter(Root, Bridge, t, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)));
            Card = new EventCardPresenter(Root, Bridge, t, null,
                photos ? AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset") : null); owned.Add(Card);
        }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }

        public VisualElement Hud => Root.Q("root");
        public Button FirstOption => Root.Q("event-options").Q<Button>();
        public VisualElement CardElement => Root.Q("event-card");
        public Button Hide => Root.Q<Button>("event-hide");
        public Button Waiting => Root.Q<Button>("event-reopen");
        public Focusable? Focused => Root.panel.focusController.focusedElement;
        public bool Shows(string name) => Root.Q(name).resolvedStyle.display == DisplayStyle.Flex;
    }

    /// <summary>A new game in the event's own time with only that event pending.</summary>
    static GameSession Pending(string eventId)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == eventId).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
        return g;
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static void Press(Button b)
    {
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
    }

    static void Covered(Rig r, bool covered, string where)
    {
        Assert.AreEqual(!covered, r.Root.Q("info-panel").enabledSelf, $"{where}: the info panel is {(covered ? "disabled" : "enabled")}");
        Assert.AreEqual(!covered, r.Root.Q("right-column").enabledSelf, $"{where}: the minimap column is {(covered ? "disabled" : "enabled")}");
    }

    static readonly (int w, int h)[] Tiers = { (1920, 1080), (960, 1080) };

    /// <summary>#380: the taller two- and three-row bars raise the centred card (the event layer keeps room at its bottom), and
    /// the backdrop must still reach the window's bottom and right edges, so no strip of map stays live under an open card.</summary>
    [UnityTest]
    public IEnumerator TheBackdropReachesTheWindowsEdgesAtEveryTier()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 1024), (1024, 1024), (960, 1080), (800, 1000), (390, 844), (375, 667) })
        {
            using var r = new Rig(w, h, "law_of_return");
            yield return Frames(4);
            var where = $"{w}x{h}";
            Assert.IsTrue(r.Shows("event-scrim"), $"{where}: the backdrop shows");
            var screen = r.Root.worldBound; var scrim = r.Root.Q("event-scrim").worldBound;
            Assert.GreaterOrEqual(scrim.yMax, screen.yMax - 0.5f, $"{where}: the backdrop {scrim} reaches the window's bottom {screen.yMax}");
            Assert.GreaterOrEqual(scrim.xMax, screen.xMax - 0.5f, $"{where}: the backdrop {scrim} reaches the window's right edge {screen.xMax}");
        }
    }

    [UnityTest]
    public IEnumerator TheOpenCardTakesThePointerAndThePanelsUnderIt()
    {
        foreach (var (w, h) in Tiers)
        {
            using var r = new Rig(w, h, "law_of_return");
            yield return Frames(4);
            var where = $"{w}x{h}";
            Assert.AreEqual(w == 960, r.Hud.ClassListContains(HudPresenter.TwoRowClass), $"{where}: precondition, the HUD tier");
            Assert.IsTrue(r.Shows("event-card"), $"{where}: the card shows");
            Assert.IsTrue(r.Hud.ClassListContains(EventCardPresenter.OpenClass) && r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            Assert.IsTrue(r.Shows("event-scrim"), $"{where}: the backdrop shows");
            Covered(r, true, where);
            // A point of the map clear of every panel: the backdrop takes it, so a map click or drag stops there.
            // (The window's left edge just under the bar: the backdrop's full-width band takes it, review of #624; beside the
            // left column's discs its strip does, review of #602; and just right of the column the backdrop itself.)
            var bar = r.Root.Q("top-bar").worldBound.yMax;
            var edge = new Vector2(4f, bar + 4f);
            Assert.AreSame(r.Root.Q("event-scrim-top"), r.Root.panel.Pick(edge), $"{where}: the backdrop's band under the bar takes the pointer at {edge}");
            var cabinet = r.Root.Q("nav-cabinet").worldBound;
            var beside = new Vector2(4f, cabinet.center.y);
            Assert.AreSame(r.Root.Q("event-gutter"), r.Root.panel.Pick(beside), $"{where}: the backdrop's strip beside the column takes the pointer at {beside}");
            var free = new Vector2(r.Root.Q("left-column").worldBound.xMax + 4f, r.Root.Q("left-column").worldBound.yMin + 4f);
            Assert.AreSame(r.Root.Q("event-scrim"), r.Root.panel.Pick(free), $"{where}: the backdrop takes the pointer at {free}");
            Assert.AreSame(r.CardElement, r.Focused, $"{where}: the new event's card has the focus, no option lit");
            Assert.IsTrue(r.FirstOption.canGrabFocus, $"{where}: its options are in reach");

            // Hide: the card collapses to "Decision waiting", which takes the focus; the map and the panels come back.
            Press(r.Hide);
            yield return Frames(3);
            Assert.IsTrue(r.Card.Collapsed, where);
            Assert.IsFalse(r.Shows("event-card"), $"{where}: the card is hidden");
            Assert.IsFalse(r.Shows("event-scrim"), $"{where}: the backdrop is gone");
            Assert.IsFalse(r.Shows("event-gutter"), $"{where}: and its strip under the left column");
            Assert.IsTrue(r.Shows("event-reopen"), $"{where}: Decision waiting shows");
            Assert.AreEqual("Decision waiting", r.Waiting.text);
            Assert.IsFalse(r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            Covered(r, false, where);
            Assert.AreSame(r.Waiting, r.Focused, $"{where}: Decision waiting has the focus");
            Assert.IsFalse(r.FirstOption.canGrabFocus, $"{where}: the hidden card's options are out of reach");
            var pick = r.Root.panel.Pick(free);
            Assert.IsTrue(pick == null || !r.Root.Q("event-layer").Contains(pick), $"{where}: the map takes the pointer again ({pick?.name})");

            // Decision waiting: the card is back, its first option focused, the panels out of reach again.
            Press(r.Waiting);
            yield return Frames(3);
            Assert.IsFalse(r.Card.Collapsed, where);
            Assert.IsTrue(r.Shows("event-card") && r.Shows("event-scrim"), $"{where}: reopened");
            Assert.IsFalse(r.Shows("event-reopen"), where);
            Covered(r, true, where);
            Assert.AreSame(r.CardElement, r.Focused, $"{where}: the reopened card has the focus");
            Assert.IsTrue(r.FirstOption.canGrabFocus, $"{where}: its options are in reach again");
        }
    }

    [UnityTest]
    public IEnumerator ANewEventOpensACollapsedCard()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        r.Card.Collapse();
        yield return Frames(2);
        Assert.IsTrue(r.Card.Collapsed);
        r.Bridge.Replace(Pending("hula_drainage"));
        yield return Frames(3);
        Assert.AreEqual("hula_drainage", r.Card.Shown?.Id);
        Assert.IsFalse(r.Card.Collapsed, "a new event opens the card again");
        Assert.IsTrue(r.Shows("event-card") && !r.Shows("event-reopen"));
        Assert.AreSame(r.CardElement, r.Focused, "and puts the focus on the card");
    }

    [UnityTest]
    public IEnumerator AnOpenEventDoesNotTakeTheFocusBack()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        var endTurn = r.Root.Q<Button>("end-turn");
        endTurn.Focus();
        Assert.AreSame(endTurn, r.Focused, "precondition");
        r.Card.Render();   // the bridge changed (a queued order, a refused one), the same event still open
        yield return Frames(3);
        Assert.AreSame(endTurn, r.Focused, "the top bar keeps the focus");
    }

    [UnityTest]
    public IEnumerator AnOpenScreenOrTheStartScreenHidesTheCardAndTakesItOutOfReach()
    {
        foreach (var (w, h) in Tiers)
        {
            using var r = new Rig(w, h, "law_of_return");
            yield return Frames(4);
            var where = $"{w}x{h}";
            foreach (var s in ScreenNav.Screens)
            {
                r.Nav.Open(s);
                yield return Frames(3);
                Assert.IsFalse(r.Shows("event-layer"), $"{where} {s}: the decision layer is hidden");
                Assert.IsFalse(r.Root.Q("event-layer").enabledSelf, $"{where} {s}: and disabled");
                Assert.IsFalse(r.FirstOption.canGrabFocus, $"{where} {s}: its options are out of reach");
                r.FirstOption.Focus();
                Assert.AreNotSame(r.FirstOption, r.Focused, $"{where} {s}: focus does not reach them");
                Assert.IsFalse(r.Hud.ClassListContains(EventCardPresenter.ModalClass), $"{where} {s}: no backdrop under a screen");
                // The panels beside a screen are as the screen leaves them (one row, Budget: beside it, in reach).
                if (w == 1920 && s == "budget") Covered(r, false, $"{where} {s}");
            }
            r.Nav.Open(null);
            yield return Frames(3);
            Assert.IsTrue(r.Shows("event-layer") && r.Shows("event-card"), $"{where}: closing the screen brings the card back");
            Assert.IsTrue(r.Root.Q("event-layer").enabledSelf && r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            Covered(r, true, where);

            r.Hud.AddToClassList(StartScreenPresenter.OpenClass);
            ScreenNav.DisableCovered(r.Root);
            yield return Frames(2);
            Assert.IsFalse(r.Shows("event-layer"), $"{where}: the start screen hides the layer");
            Assert.IsFalse(r.Root.Q("event-layer").enabledSelf, $"{where}: and disables it");
            Assert.IsFalse(r.Hud.ClassListContains(EventCardPresenter.ModalClass), where);
            r.Hud.RemoveFromClassList(StartScreenPresenter.OpenClass);
            ScreenNav.DisableCovered(r.Root);
        }
    }

    /// <summary>The longest refusal a toast shows, three to four lines in the toast's width.</summary>
    const string LongToast = "Not done: Not enough attention (need 4.0, have 2.0)\nNot done: No more than 2 map actions in one turn\n"
        + "Not done: The Knesset must approve it: the coalition holds 46 of 120 seats\nYour game was saved, but the browser refused the autosave";

    /// <summary>The toast moves to the top middle while the card is open: over the photograph, clear of its CC BY credit,
    /// the title and the options, with the real photographs, a long refusal, and on the windows where the card fills the
    /// layer (21:9 and a short laptop window) as well as the one-row and two-row HUDs.</summary>
    [UnityTest]
    public IEnumerator TheToastClearsTheOpenCardsCreditTitleAndOptions()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (960, 1080), (2560, 1080), (1366, 657) })
            foreach (var ev in new[] { "law_of_return", "operation_hiram" })   // a short text; the tallest card
            {
                using var r = new Rig(w, h, ev, photos: true);
                yield return Frames(5);
                var where = $"{w}x{h} {ev}";
                Assert.IsTrue(r.Card.PhotoAlt != null, $"{where}: precondition: the photograph shows");
                var toast = r.Root.Q<Label>("toast");
                toast.text = LongToast;
                toast.style.display = DisplayStyle.Flex;
                yield return Frames(3);
                var t = toast.worldBound;
                foreach (var name in new[] { "event-photo-credit", "event-title", "event-options", "event-hide" })
                    Assert.IsFalse(t.Overlaps(r.Root.Q(name).worldBound), $"{where}: the toast {t} covers {name} {r.Root.Q(name).worldBound}");
                Assert.Greater(r.Root.Q("root").IndexOf(toast), r.Root.Q("root").IndexOf(r.Root.Q("event-layer")), "the toast is drawn above the layer");
            }
    }

    /// <summary>Review S1: the card takes the focus when the start screen closes, and when a screen that covered it closes,
    /// whether the event was open before the screen or arrived under it; a new session (New Game, a load) forgets what
    /// was collapsed (N5).</summary>
    [UnityTest]
    public IEnumerator TheCardTakesTheFocusWhenItsCoverCloses()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        r.Nav.Open("cabinet");
        yield return Frames(3);
        Assert.AreNotSame(r.CardElement, r.Focused, "precondition: covered");
        r.Nav.Open(null);
        yield return Frames(3);
        Assert.AreSame(r.CardElement, r.Focused, "closing Cabinet gives the open card the focus");

        // An event that arrives while a screen is open takes the focus when it closes.
        r.Nav.Open("budget");
        yield return Frames(2);
        r.Bridge.Replace(Pending("hula_drainage"));
        yield return Frames(3);
        Assert.AreNotSame(r.CardElement, r.Focused, "precondition: covered when it arrived");
        r.Nav.Close();
        yield return Frames(3);
        Assert.AreEqual("hula_drainage", r.Card.Shown?.Id);
        Assert.AreSame(r.CardElement, r.Focused, "the event that arrived under Budget has the focus once Budget closes");

        // The start screen, as at boot: shown over an open card, then closed.
        r.Hud.AddToClassList(StartScreenPresenter.OpenClass);
        ScreenNav.DisableCovered(r.Root);
        r.Root.Q<Button>("end-turn").Focus();   // what the start screen's own button stands for
        yield return Frames(3);
        r.Hud.RemoveFromClassList(StartScreenPresenter.OpenClass);
        ScreenNav.DisableCovered(r.Root);
        yield return Frames(3);
        Assert.AreSame(r.CardElement, r.Focused, "closing the start screen gives the open card the focus");

        // N5: a collapsed card, then a new session with the same event: it opens.
        r.Card.Collapse();
        yield return Frames(2);
        Assert.IsTrue(r.Card.Collapsed);
        r.Bridge.Replace(Pending("hula_drainage"));
        yield return Frames(3);
        Assert.IsFalse(r.Card.Collapsed, "a new session forgets the collapse");
        Assert.AreSame(r.CardElement, r.Focused, "and the card has the focus");
    }

    /// <summary>N1 and N2: Hide with the pointer leaves no focus fill on "Decision waiting" (PointerBlur's rule), with the
    /// keyboard it moves the focus there; Escape on the open card is Hide, and does nothing under a screen.</summary>
    [UnityTest]
    public IEnumerator HideByPointerLeavesNoFocusAndEscapeHides()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        using (var down = PointerDownEvent.GetPooled()) { down.target = r.Hide; r.Hide.SendEvent(down); }
        r.Card.Collapse();   // what the click's Clickable does next
        yield return Frames(3);
        Assert.IsTrue(r.Card.Collapsed);
        Assert.AreNotSame(r.Waiting, r.Focused, "a pointer Hide leaves Decision waiting unfocused");
        Press(r.Waiting);
        yield return Frames(3);
        Assert.IsFalse(r.Card.Collapsed);

        using (var cancel = NavigationCancelEvent.GetPooled()) { cancel.target = r.CardElement; r.CardElement.SendEvent(cancel); }
        yield return Frames(3);
        Assert.IsTrue(r.Card.Collapsed, "Escape on the open card hides it");
        Assert.AreSame(r.Waiting, r.Focused, "and, from the keyboard, Decision waiting has the focus");
        Assert.IsFalse(r.Card.Escape(), "Escape on a collapsed card does nothing");
        Press(r.Waiting);
        yield return Frames(2);
        r.Nav.Open("why");
        yield return Frames(2);
        Assert.IsFalse(r.Card.Escape(), "under a screen Escape is the screen's");
        Assert.IsFalse(r.Card.Collapsed);
    }

    /// <summary>#41 M1: the last open event answered with the mouse hands no focus on (PointerBlur's rule), so End Turn is
    /// not lit.</summary>
    [UnityTest]
    public IEnumerator AnsweringTheLastEventWithThePointerFocusesNothing()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        PressWithPointer(r.FirstOption);
        yield return Frames(3);
        Assert.IsNull(r.Card.Shown, "precondition: the click answered the only event");
        Assert.AreNotSame(r.Root.Q<Button>("end-turn"), r.Focused, "End Turn is not lit by a mouse click");
    }

    /// <summary>A mouse press as the player makes it: pointer down (which also gives the button the focus), then the
    /// button's Clickable fires `clicked` on pointer up. UiTestKit.Press is the keyboard's Submit instead.</summary>
    static void PressWithPointer(Button b)
    {
        b.Focus();
        using (var down = PointerDownEvent.GetPooled()) { down.target = b; b.SendEvent(down); }
        using var click = ClickEvent.GetPooled();
        click.target = b;
        typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(EventBase) }, null)!.Invoke(b.clickable, new object[] { click });
    }

    /// <summary>#41 M1: Escape from a screen over an open card gives the card the focus (not the screen's button), and the
    /// last open event answered from the keyboard gives it to End Turn, so the next Enter or Tab goes on from there.</summary>
    [UnityTest]
    public IEnumerator AnsweringTheLastEventByKeyboardFocusesEndTurn()
    {
        using var r = new Rig(1920, 1080, "law_of_return");
        yield return Frames(4);
        r.Nav.Open("budget");
        yield return Frames(3);
        var reset = r.Root.Q<Button>("budget-reset");
        reset.Focus();
        yield return Frames(2);
        Assert.AreSame(reset, r.Focused, "precondition: a control on the screen has the focus");
        using (var cancel = NavigationCancelEvent.GetPooled()) { cancel.target = reset; reset.SendEvent(cancel); }
        yield return Frames(3);
        Assert.IsNull(r.Nav.Current);
        Assert.AreSame(r.CardElement, r.Focused, "the open card takes the focus when the screen over it closes");

        var option = r.FirstOption;
        option.Focus();
        yield return Frames(2);
        Assert.AreSame(option, r.Focused, "precondition: an option has the focus");
        Press(option);
        yield return Frames(3);
        Assert.IsNull(r.Card.Shown, "the only event is answered");
        Assert.AreSame(r.Root.Q<Button>("end-turn"), r.Focused, "End Turn has the focus");
    }

    /// <summary>#849, the owner's decision of 17 October 2026 ("Hide legend under card", on desktop and phone alike;
    /// issuecomment-6233870812): while the modal decision card is open the map's legend (map-layer) is hidden, so no card,
    /// the photograph's included, covers it; Hide brings it back, Decision waiting hides it again, and answering the event
    /// brings it back. Only the legend: the minimap and the left column stay shown. Every window ArmisticeTests lists, in
    /// English and Hebrew.</summary>
    [UnityTest]
    public IEnumerator TheLegendIsHiddenWhileTheCardIsOpen()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var problems = new List<string>();
        foreach (var t in new[] { Text, he })
        foreach (var (w, h) in new[] { (1920, 1080), (1680, 1050), (1280, 720), (1531, 784), (1366, 657), (2560, 1080), (1920, 900), (1280, 1024),
                                       (960, 600), (1024, 768), (390, 844), (1600, 757), (1024, 1024), (960, 1080), (800, 1000) })
        {
            using var r = new Rig(w, h, "idf_founding", photos: true, text: t);
            var legend = r.Root.Q<Label>("map-layer");
            legend.text = "Held by Israel: blue"; legend.style.display = DisplayStyle.Flex;   // as GameRoot shows it while borders are drawn
            yield return Frames(4);
            var where = $"{t["ui.lang"]} {w}x{h}";
            bool Seen() => legend.resolvedStyle.display == DisplayStyle.Flex && legend.resolvedStyle.visibility == Visibility.Visible;
            void Expect(bool seen, string when)
            {
                if (Seen() != seen) problems.Add($"{where}: the legend is {(seen ? "hidden" : "shown")} {when}");
            }
            if (!r.Shows("event-card")) { problems.Add($"{where}: precondition, the card shows"); continue; }
            Expect(false, "under the open card");
            foreach (var n in new[] { "minimap", "left-column" })
                if (r.Root.Q(n).resolvedStyle.visibility != Visibility.Visible || !r.Shows(n)) problems.Add($"{where}: {n} is hidden with the legend");
            Press(r.Hide);
            yield return Frames(3);
            Expect(true, "after Hide");
            Press(r.Waiting);
            yield return Frames(3);
            Expect(false, "with the card reopened");
            Press(r.FirstOption);
            yield return Frames(3);
            if (r.Card.Shown != null) problems.Add($"{where}: precondition, the option answered the event");
            Expect(true, "once the event is answered");
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
