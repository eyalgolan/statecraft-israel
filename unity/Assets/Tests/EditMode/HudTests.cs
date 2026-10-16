#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class HudTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge NewBridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);

    [Test]
    public void TheTopBarShowsTheOpeningOfTheGame()
    {
        var m = HudModel.From(NewBridge(), Text);
        Assert.AreEqual("1948", m.Year);
        Assert.AreEqual("Turn 0", m.Turn);
        Assert.AreEqual("15 May 1948", m.Date);
        Assert.AreEqual("IL 20.0m", m.Treasury.Value);          // Israeli pounds (playtest 1, item 7)
        Assert.AreEqual("$40.0m", m.Fx.Value);
        Assert.AreEqual("30%", m.Security.Value);                    // the 0-100 scale, with its unit (fix round 1, I2)
        Assert.AreEqual("50%", m.Standing.Value);
        StringAssert.EndsWith("%", m.Support.Value);
        Assert.AreEqual("Level 5 of 5", m.SecurityLevel);
        Assert.AreEqual("Attention 4.0 of 4.0", m.Attention);
        Assert.AreEqual("", m.Treasury.Delta);                       // no previous turn yet
    }

    /// <summary>T71: the top bar's Treasury is the net position, cash minus debt (HudModel.From; M3 plan: "with deficits
    /// borrowed, cash alone is always 0"). The sim keeps cash at 0 or above (R-MONEY-003), so a negative reading is debt,
    /// not a negative Treasury: after four monthly turns with no orders, 0 cash and the debt read as minus the debt.</summary>
    [Test]
    public void TheTreasuryShowsCashMinusDebtSoBorrowingReadsNegative()
    {
        var bridge = NewBridge();
        for (var i = 0; i < 4; i++) bridge.EndTurn();
        Assert.AreEqual(0.0, bridge.State.Treasury);   // R-MONEY-003: cash stops at 0
        Assert.Greater(bridge.State.Debt, 0.0);
        var m = HudModel.From(bridge, Text);
        Assert.AreEqual(PlayerText.Money(C, "treasury", -bridge.State.Debt), m.Treasury.Value);
        StringAssert.Contains("-IL", m.Treasury.Value);
    }

    // ---- The score (#380): only in the GDD loop, from GameState.Score ----

    /// <summary>The real game: the GDD loop since the cutover (the ?mvp=1 slice is gone with MvpSlice).</summary>
    static SimBridge GddBridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);

    [Test]
    public void TheLegacyLoopShowsNoScore()
    {
        // The legacy loop survives only as content without the loop switch (SimBridgeGddTests.Legacy) since the cutover.
        SimBridge LegacyBridge() => new SimBridge(GameSession.New(SimBridgeGddTests.Legacy, GoldenRun.Seed, Mode.Historical), Text);
        Assert.AreEqual("", HudModel.From(LegacyBridge(), Text).Score);
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, LegacyBridge(), Text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("score").style.display.value);
    }

    [Test]
    public void TheGddGameShowsItsScore()
    {
        var bridge = GddBridge();
        Assert.AreEqual("Score 0", HudModel.From(bridge, Text).Score);
        bridge.State.Score = 150;
        Assert.AreEqual("Score 150", HudModel.From(bridge, Text).Score);
        // The value carries left-to-right marks in a Hebrew line (U+200E, #543), as every Hebrew value does.
        var he = HudModel.From(bridge, He).Score;
        Assert.AreEqual("ניקוד \u200E150\u200E", he);
        Assert.AreEqual("ניקוד 150", he.Replace("\u200E", ""));
    }

    [Test]
    public void TheBarShowsTheGddGamesScore()
    {
        using var ui = new UiTestKit();
        var bridge = GddBridge();
        using var hud = new HudPresenter(ui.Root, bridge, Text);
        var label = ui.Q<Label>("score");
        Assert.AreEqual("Score 0", label.text);
        Assert.AreEqual(DisplayStyle.Flex, label.style.display.value);
        bridge.State.Score = 150;
        hud.Render();
        Assert.AreEqual("Score 150", label.text);
    }

    [Test]
    public void EndTurnAdvancesTheGameAndShowsChanges()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new HudPresenter(ui.Root, bridge, Text);
        ui.Press("end-turn");
        Assert.AreEqual(1, bridge.State.Turn);
        Assert.AreEqual("Turn 1", ui.Q<Label>("turn").text);
        Assert.AreEqual("15 June 1948", ui.Q<Label>("date").text);   // monthly turns since the GDD cutover
        var arrivals = bridge.Session.Last!.Why.Where(w => w.System == "population").Sum(w => w.Delta);
        Assert.AreEqual(arrivals.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), ui.Q<Label>("immigration-value").text);
        var s0 = bridge.Session.Previous!; var s1 = bridge.State;
        var d = System.Math.Round((s1.Treasury - s1.Debt) - (s0.Treasury - s0.Debt), 1, System.MidpointRounding.AwayFromZero);
        Assert.AreNotEqual(0.0, d, "the test needs a turn with a treasury change");
        Assert.AreEqual(PlayerText.MoneyChange(C, "treasury", d), ui.Q<Label>("treasury-delta").text);
        Assert.AreEqual(d < 0, ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
        Assert.AreEqual(d > 0, ui.Q<Label>("treasury-delta").ClassListContains("delta-up"));
    }

    [Test]
    public void QueuedOrdersSpendAttentionUpFront()
    {
        var bridge = NewBridge();
        Assert.IsNull(bridge.Queue(new SetMobilization(1.0)));      // at war each order costs 2
        Assert.AreEqual(2.0, bridge.AttentionLeft, 1e-9);
        Assert.IsNull(bridge.Queue(new SetMobilization(0.9)));      // replaces the first: still one order
        Assert.AreEqual(1, bridge.Queued.Count);
        Assert.IsNull(bridge.Queue(new SecurityOrder(-1)));
        Assert.AreEqual("Not enough attention (need 2.0, have 0.0)", bridge.Queue(new SetPolicy("rationing", 1)));
    }

    [Test]
    public void RefusedOrdersAreShownWithTheirReason()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var text = Text;
        new HudPresenter(ui.Root, bridge, text);
        new ToastPresenter(ui.Root, bridge, text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
        bridge.Queue(new SetPolicy(C.Policies[0].Id, 2));     // a known policy at an impossible level: the sim refuses it
        ui.Press("end-turn");
        Assert.AreEqual("Not done: Policy level must be between 0 and 1", ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("toast").style.display.value);
    }

    [Test]
    public void NavigationOpensOneScreenAtATime()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        Assert.IsNull(nav.Current);
        ui.Press("nav-budget");
        Assert.AreEqual("budget", nav.Current);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("screen-budget").style.display.value);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-cabinet").style.display.value);
        ui.Press("nav-cabinet");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-budget").style.display.value);
        ui.Press("nav-cabinet");
        Assert.IsNull(nav.Current);
    }

    [UnityTest]
    public IEnumerator AtStartNoScreenIsShownAndTheMiddleOfTheScreenIsNotUi()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new ScreenNav(ui.Root);
        new ToastPresenter(ui.Root, bridge, Text);
        foreach (var name in new[] { "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>(name).style.display.value, name);
        for (int i = 0; i < 10 && float.IsNaN(ui.Root.worldBound.width); i++) yield return null;
        var panel = ui.Root.panel;
        var middle = ui.Root.worldBound.center;
        Assert.IsNull(panel.Pick(middle), "the middle of the screen must reach the map");
        // screen coordinates are y-up; UiPointer flips them
        var screenH = (float)Screen.height;
        var asScreen = new Vector2(middle.x, screenH - middle.y);
        Assert.IsFalse(UiPointer.IsOverUi(panel, asScreen, screenH));
    }

    [Test]
    public void BadIdsAreRefusedNotThrownAndTheNextTurnRuns()
    {
        var bridge = NewBridge();
        var refusals = new System.Collections.Generic.List<string>();
        bridge.Refused += refusals.Add;
        Assert.IsNotNull(bridge.Queue(new SetPolicy(null!, 1)));
        Assert.IsNotNull(bridge.Queue(new SetPolicy("", 1)));
        Assert.IsNotNull(bridge.Queue(new SetPolicy("curfew", 1)));
        Assert.IsNotNull(bridge.Queue(new Appoint("pm", "free text")));
        Assert.IsNotNull(bridge.Queue(new Appoint(null!, null!)));
        Assert.IsNotNull(bridge.Queue(new ChooseEventOption("nope", "nope")));
        Assert.IsNotNull(bridge.Queue(new ChooseEventOption(C.Events[0].Id, "nope")));
        Assert.IsNotNull(bridge.Queue(new MapAction("moat", 0, 0)));
        Assert.IsNotNull(bridge.Queue(new AllocateBudget(new System.Collections.Generic.Dictionary<string, double> { ["nowhere"] = 1 })));
        Assert.IsNotNull(bridge.Queue(new AllocateBudget(null!)));
        Assert.AreEqual(0, bridge.Queued.Count);
        Assert.AreEqual(10, refusals.Count);
        Assert.DoesNotThrow(() => bridge.EndTurn());
        Assert.AreEqual(1, bridge.State.Turn);
    }

    [Test]
    public void AQueuedBudgetIsACopy()
    {
        var bridge = NewBridge();
        var shares = new System.Collections.Generic.Dictionary<string, double>(C.Rules.BudgetStart);
        Assert.IsNull(bridge.Queue(new AllocateBudget(shares)));
        var key = shares.Keys.First();
        shares[key] = 99;
        shares["nowhere"] = 1;
        var queued = (AllocateBudget)bridge.Queued[0];
        Assert.AreEqual(C.Rules.BudgetStart[key], queued.Shares[key], 1e-12);
        Assert.IsFalse(queued.Shares.ContainsKey("nowhere"));
    }

    [Test]
    public void ADisposedPresenterHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var hud = new HudPresenter(ui.Root, bridge, Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var nav = new ScreenNav(ui.Root);
        hud.Dispose(); toast.Dispose(); nav.Dispose();
        ui.Q<Label>("turn").text = "untouched";
        ui.Q<Label>("toast").text = "untouched";
        bridge.Queue(new SetPolicy("curfew", 1));
        bridge.EndTurn();
        ui.Press("end-turn");
        ui.Press("nav-budget");
        Assert.AreEqual("untouched", ui.Q<Label>("turn").text);
        Assert.AreEqual("untouched", ui.Q<Label>("toast").text);
        Assert.AreEqual(1, bridge.State.Turn, "End Turn button no longer wired (only the direct call ran)");
        Assert.IsNull(nav.Current);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-budget").style.display.value);
    }

    [Test]
    public void ToastShowsQueueRefusalsAndClearsOnTheNextChange()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new ToastPresenter(ui.Root, bridge, Text);
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("unknown policy 'curfew'"));   // the id: log only (#622)
        Assert.IsNotNull(bridge.Queue(new SetPolicy("curfew", 1)));
        Assert.AreEqual("Not done: Unknown policy", ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("toast").style.display.value);
        Assert.IsNull(bridge.Queue(new SecurityOrder(-1)));
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
    }

    [Test]
    public void ChangesShowSignsAndZeroDeltasAreEmpty()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        bridge.EndTurn();
        var hud = new HudPresenter(ui.Root, bridge, Text);
        var prev = bridge.Session.Previous!;
        var now = bridge.State;
        prev.Treasury = now.Treasury - now.Debt + prev.Debt - 5;          // up by 5
        hud.Render();
        Assert.AreEqual("+IL 5.0m", ui.Q<Label>("treasury-delta").text);
        Assert.IsTrue(ui.Q<Label>("treasury-delta").ClassListContains("delta-up"));
        Assert.IsFalse(ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
        prev.Treasury = now.Treasury - now.Debt + prev.Debt + 0.04;       // down by 0.04: rounds to zero, never "-0.0"
        hud.Render();
        Assert.AreEqual("", ui.Q<Label>("treasury-delta").text);
        Assert.IsFalse(ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
        prev.Treasury = now.Treasury - now.Debt + prev.Debt - 0.04;
        hud.Render();
        Assert.AreEqual("", ui.Q<Label>("treasury-delta").text);
    }

    [Test]
    public void TheHudIsTheSameUnderAGermanCulture()
    {
        var bridge = NewBridge();
        bridge.EndTurn();
        var inv = HudModel.From(bridge, Text);
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var de = HudModel.From(bridge, Text);
            Assert.AreEqual(JsonUtility.ToJson(Flat(inv)), JsonUtility.ToJson(Flat(de)));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }

    [System.Serializable] class FlatHud { public string[] V = new string[0]; }
    static FlatHud Flat(HudModel m) => new FlatHud
    {
        V = new[] { m.Year, m.Turn, m.Date, m.Attention, m.SecurityLevel,
            m.Treasury.Value, m.Treasury.Delta, m.Immigration.Value, m.Immigration.Delta, m.Security.Value, m.Security.Delta,
            m.Support.Value, m.Support.Delta, m.Standing.Value, m.Standing.Delta, m.Fx.Value, m.Fx.Delta },
    };

    [Test]
    public void ReplaceDropsOrdersAndBeforeLastSpansTwoTurns()
    {
        var bridge = NewBridge();
        Assert.IsNull(bridge.BeforeLast);
        bridge.EndTurn();
        Assert.IsNull(bridge.BeforeLast);                        // only one step has run
        var first = bridge.Session.Last;
        bridge.EndTurn();
        Assert.AreSame(first, bridge.BeforeLast);
        Assert.AreNotSame(first, bridge.Session.Last);
        bridge.Queue(new SecurityOrder(-1));
        int changed = 0; bridge.Changed += () => changed++;
        bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        Assert.AreEqual(1, changed);
        Assert.AreEqual(0, bridge.Queued.Count);
        Assert.IsNull(bridge.BeforeLast);
        Assert.AreEqual(0, bridge.State.Turn);
    }

    /// <summary>#622: a refusal names the kind of thing and never shows the content id or the exception's message, in
    /// either language; both stay in the log for a bug report.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ARefusalShowsNoIdOrExceptionTextInEitherLanguage(bool hebrew)
    {
        var text = hebrew ? UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml") : Text;
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("unknown policy 'curfew'"));
        var unknown = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text).Queue(new SetPolicy("curfew", 1));
        Assert.AreEqual(text.Format("order.unknown", text["order.kind.policy"]), unknown);   // the kind in the UI's language (#595)
        StringAssert.DoesNotContain("curfew", unknown);

        LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("stack detail 42"));
        var failing = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text,
            (s, cmds) => throw new System.InvalidOperationException("stack detail 42"));
        Assert.IsNull(failing.Queue(new SecurityOrder(-1)));
        var reason = failing.EndTurn().Rejected.Single().Reason;
        Assert.AreEqual(text["order.failed"], reason);
        StringAssert.DoesNotContain("stack detail", reason);
    }

    /// <summary>Any exception from the sim (not only ArgumentException; final review M7) keeps the state, clears the queue
    /// and shows the reason: End Turn can never soft-lock. An unexpected kind is also logged in full.</summary>
    [TestCase(typeof(System.ArgumentException))]
    [TestCase(typeof(System.InvalidOperationException))]
    [TestCase(typeof(System.NullReferenceException))]
    public void ABatchTheSimRefusesOutrightKeepsTheStateAndShowsTheReason(System.Type kind)
    {
        using var ui = new UiTestKit();
        // Every kind leaves a console trace (rereview): a refusal as a warning, anything else as the full exception.
        if (kind == typeof(System.ArgumentException)) LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("End Turn refused.*boom"));
        else LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text,
            (s, cmds) => throw (System.Exception)System.Activator.CreateInstance(kind, "boom")!);
        new ToastPresenter(ui.Root, bridge, Text);
        var order = new SecurityOrder(-1);
        Assert.IsNull(bridge.Queue(order));
        System.Collections.Generic.IReadOnlyList<Rejection>? seen = null;
        bridge.TurnEnded += r => seen = r;
        var hash = bridge.State.Hash();
        Assert.DoesNotThrow(() => bridge.EndTurn());
        Assert.AreEqual(hash, bridge.State.Hash());
        Assert.AreEqual(0, bridge.Queued.Count);
        Assert.IsNull(bridge.BeforeLast);
        Assert.AreSame(order, seen![0].Command);
        Assert.AreEqual("Not done: The turn could not run. Your orders were cleared. Give them again.", ui.Q<Label>("toast").text);   // "boom": the log only (#622)
    }

    /// <summary>Playtest 1, item 11: pressing the open screen's button closes it, every time, with every presenter
    /// GameRoot creates listening, with the button focused (as after a mouse click), and across End Turn.</summary>
    [Test]
    public void PressingTheOpenScreensButtonClosesItAcrossTurns()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var portraits = ScriptableObject.CreateInstance<PortraitCatalog>();
        var owned = new System.Collections.Generic.List<System.IDisposable>();   // fix round 1: nothing outlives the test
        T Own<T>(T d) where T : System.IDisposable { owned.Add(d); return d; }
        try
        {
            Own(new HudPresenter(ui.Root, bridge, Text));
            var nav = Own(new ScreenNav(ui.Root));
            Own(new EventCardPresenter(ui.Root, bridge, Text, portraits));
            Own(new DefencePresenter(ui.Root, bridge, Text));
            Own(new BudgetPresenter(ui.Root, bridge, Text));
            Own(new WhyPresenter(ui.Root, bridge, Text));
            Own(new CabinetPresenter(ui.Root, bridge, Text, portraits));
            DisplayStyle Shown(string screen) => ui.Q<VisualElement>("screen-" + screen).style.display.value;
            for (int turn = 0; turn < 4; turn++)
            {
                foreach (var screen in ScreenNav.Screens)
                {
                    ui.Press("nav-" + screen);
                    Assert.AreEqual(DisplayStyle.Flex, Shown(screen), $"turn {turn}: {screen} did not open");
                    ui.Q<Button>("nav-" + screen).Focus();
                    ui.Press("nav-" + screen);
                    Assert.AreEqual(DisplayStyle.None, Shown(screen), $"turn {turn}: {screen} did not close");
                    Assert.IsNull(nav.Current);
                }
                var open = ScreenNav.Screens[turn % ScreenNav.Screens.Length];
                ui.Press("nav-" + open);
                ui.Press("end-turn");
                Assert.AreEqual(turn + 1, bridge.State.Turn, $"turn {turn}: End Turn did not run");
                Assert.AreEqual(DisplayStyle.Flex, Shown(open), $"turn {turn}: End Turn closed {open}");
                ui.Press("nav-" + open);
                Assert.AreEqual(DisplayStyle.None, Shown(open), $"turn {turn}: {open} did not close after End Turn");
            }
        }
        finally
        {
            DisposeAll(owned);
            Object.DestroyImmediate(portraits);
        }
        // Every presenter let go of the bridge and the buttons: a later turn writes nothing and End Turn does nothing.
        ui.Q<Label>("turn").text = "untouched";
        ui.Q<Label>("cabinet-coalition").text = "untouched";
        bridge.EndTurn();
        var turnAfter = bridge.State.Turn;
        ui.Press("end-turn");
        Assert.AreEqual("untouched", ui.Q<Label>("turn").text, "HudPresenter still listens");
        Assert.AreEqual("untouched", ui.Q<Label>("cabinet-coalition").text, "CabinetPresenter still listens");
        Assert.AreEqual(turnAfter, bridge.State.Turn, "End Turn is still wired");
    }

    /// <summary>Disposes test-owned presenters newest first, as GameRoot does.</summary>
    static void DisposeAll(System.Collections.Generic.List<System.IDisposable> owned)
    {
        for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
        owned.Clear();
    }

    /// <summary>Fix round 1, I2: the top-bar changes carry their unit: IL for the treasury, $ for foreign currency, % for
    /// the 0-100 meters.</summary>
    [Test]
    public void EveryTopBarChangeCarriesItsUnit()
    {
        var bridge = NewBridge();
        bridge.EndTurn();
        var prev = bridge.Session.Previous!;
        var now = bridge.State;
        prev.FxReserves = now.FxReserves + 2;                                // down by 2
        prev.Indicators["security"] = now.Indicators["security"] - 0.05;    // up 5 points
        prev.Treasury = now.Treasury - now.Debt + prev.Debt + 3;            // down by 3
        var m = HudModel.From(bridge, Text);
        Assert.AreEqual("-$2.0m", m.Fx.Delta);
        Assert.AreEqual("+5%", m.Security.Delta);
        Assert.AreEqual("-IL 3.0m", m.Treasury.Delta);
    }

    /// <summary>Fix round 2 (N4, N5): no script rescales a sim number for display itself: 0..1 values go through
    /// PlayerText.Scale / Percent / PercentChange. Scans all of Assets/Scripts for "* 100", "100f", "/ 0.01" and "100.0".
    /// Allowed, each for a reason that is not a displayed number:</summary>
    static readonly (string file, string contains, string why)[] AllowedScale =
    {
        ("MinimapPresenter.cs", "Length.Percent(", "the view frame's position and size in percent of the picture (layout, not text)"),
        ("PortraitFraming.cs", "offset / (h - boxH) * 100f", "a portrait's background-position-y in percent (layout, not text)"),
        ("UiBar.cs", "Length.Percent(", "a bar's fill width and tick position in percent of its track (layout, not text)"),
        ("MapShaderGlobals.cs", "RiftHazeTopM = -100f", "a real elevation in metres for the rift haze (shader input, not a displayed number)"),
        ("SettlementKits.cs", "DefaultHouses = 100f", "the owner's symbolic scale for houses and trees (100x real size), not a displayed number"),
        ("RouteLayer.cs", "CorridorMarginM = 100f", "a clearance in map metres that kit buildings and trees keep from a route (geometry, not a displayed number)"),
        ("CameraController.cs", "PixelsPerNotch = 100f", "wheel pixels in one mouse notch, to scale the browser's wheel events (input, not a displayed number)"),
    };

    [Test]
    public void NoScriptRescalesADisplayedNumberItself()
    {
        var pattern = new System.Text.RegularExpressions.Regex(@"\*\s*100\b|\b100f\b|/\s*0\.01\b|\b100\.0\b|\*\s*100f");
        foreach (var f in System.IO.Directory.GetFiles("Assets/Scripts", "*.cs", System.IO.SearchOption.AllDirectories))
            foreach (var line in System.IO.File.ReadAllLines(f))
                if (pattern.IsMatch(line) && !System.Array.Exists(AllowedScale, a => System.IO.Path.GetFileName(f) == a.file && line.Contains(a.contains)))
                    Assert.Fail($"{System.IO.Path.GetFileName(f)}: {line.Trim()}");
        foreach (var a in AllowedScale) Assert.IsNotEmpty(a.why);
    }

    /// <summary>Fix round 2 (N4): percentages have one formatter, PlayerText; no UI string formats a number as "%" itself.</summary>
    [Test]
    public void NoUiStringFormatsAPercentItself()
    {
        var yaml = AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text;
        foreach (var line in yaml.Split('\n'))
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(line, @"\{\d+:[^}]*\}%"), "percent format in strings.en.yaml: " + line);
    }

    /// <summary>Fix round 1, M3: the top-bar captions are the content names. Since #622 they are in the sentence case the
    /// content and the rest of the strings use, so a caption is the content name as written (it was Title Case before);
    /// Immigration and Public support are UI quantities with no content name.</summary>
    [Test]
    public void TopBarCaptionsAreTheContentNames()
    {
        using var ui = new UiTestKit();
        string Caption(string meter) => ui.Q<VisualElement>(meter).Q<Label>(className: "meter-name").text;
        Assert.AreEqual(PlayerText.Target(C, "treasury"), Caption("meter-treasury"));
        Assert.AreEqual(PlayerText.Target(C, "security"), Caption("meter-security"));
        Assert.AreEqual(PlayerText.Target(C, "intl_standing"), Caption("meter-standing"));
        Assert.AreEqual(PlayerText.Target(C, "fx_reserves"), Caption("meter-fx"));
    }

    /// <summary>Fix round 2 (R2-3, playtest 1 item 11): two Submit events on the Defence button within one frame (a
    /// double click, or a click plus Enter) each toggle the screen, as ScreenNav documents: no debounce. From closed the
    /// result is closed, from open it is open; the same input always gives the same state.</summary>
    [Test]
    public void TwoPressesInOneFrameToggleTwice()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        try
        {
            var defence = ui.Q<Button>("nav-defence");
            var frame = Time.frameCount;
            UiTestKit.Press(defence); UiTestKit.Press(defence);
            Assert.AreEqual(frame, Time.frameCount, "both presses in the same frame");
            Assert.IsNull(nav.Current, "closed -> open -> closed");
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-defence").style.display.value);
            UiTestKit.Press(defence);                                   // open it
            UiTestKit.Press(defence); UiTestKit.Press(defence);
            Assert.AreEqual("defence", nav.Current, "open -> closed -> open");
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("screen-defence").style.display.value);
        }
        finally { nav.Dispose(); }
    }

    /// <summary>Fix round 3 (m-1): the top bar's change agrees with the two values it shows.</summary>
    [Test]
    public void TheShownChangeAgreesWithTheShownValues()
    {
        var bridge = NewBridge();
        bridge.EndTurn();
        var prev = bridge.Session.Previous!;
        var now = bridge.State;
        now.Indicators["security"] = 0.634; prev.Indicators["security"] = 0.626;     // "63%" -> "63%"
        var m = HudModel.From(bridge, Text);
        Assert.AreEqual("63%", m.Security.Value);
        Assert.AreEqual("", m.Security.Delta, "no change shown between two equal shown values");
        Assert.AreEqual(0, m.Security.Sign);
        now.Indicators["security"] = 0.626; prev.Indicators["security"] = 0.624;     // "62%" -> "63%"
        m = HudModel.From(bridge, Text);
        Assert.AreEqual("63%", m.Security.Value);
        Assert.AreEqual("+1%", m.Security.Delta);
        Assert.AreEqual(1, m.Security.Sign);
    }

    static readonly string[] MeterIds = { "treasury", "immigration", "security", "support", "standing", "fx" };
    static string Inv(System.FormattableString f) => System.FormattableString.Invariant(f);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings.Replace("strings.en.yaml", "strings.he.yaml")).text, "strings.he.yaml");

    /// <summary>Ends turns until Immigration shows a change (it compares two turns' arrivals), so every meter can show one.
    /// Fails if none comes within 30 turns, so a test built on it never passes by checking nothing.</summary>
    static void PlayUntilImmigrationChanges(SimBridge bridge)
    {
        for (int i = 0; i < 30 && HudModel.From(bridge, Text).Immigration.Sign == 0; i++) bridge.EndTurn();
        Assert.AreNotEqual(0, HudModel.From(bridge, Text).Immigration.Sign, "no Immigration change within 30 turns");
    }

    /// <summary>#398: each meter's tooltip says its value and the change since last turn in words: money in millions
    /// with its currency, the 0-100 meters in points, a count as shown; with no change (turn 0, or one that rounds to
    /// zero) only the caption and the value. The caption is the strings file's (the UI's language).</summary>
    [Test]
    public void EachMeterSaysItsValueAndTheChangeSinceLastTurn()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var hud = new HudPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("Money: IL 20.0 million", ui.Q<VisualElement>("meter-treasury").tooltip);           // turn 0: no change
        Assert.AreEqual("Foreign currency: $40.0 million", ui.Q<VisualElement>("meter-fx").tooltip);
        Assert.AreEqual("Security: 30%", ui.Q<VisualElement>("meter-security").tooltip);
        bridge.EndTurn();
        var prev = bridge.Session.Previous!; var now = bridge.State;
        prev.Treasury = now.Treasury - now.Debt + prev.Debt + 1.4;            // down by 1.4
        prev.FxReserves = now.FxReserves - 2;                                // up by 2
        prev.Indicators["security"] = now.Indicators["security"] - 0.05;    // up 5 points
        prev.Indicators["intl_standing"] = now.Indicators["intl_standing"] + 0.01;   // down 1 point
        hud.Render();
        var net = System.Math.Round(now.Treasury - now.Debt, 1, System.MidpointRounding.AwayFromZero);
        Assert.Greater(net, 0, "the test expects a positive net position after one turn");
        Assert.AreEqual(Inv($"Money: IL {net:0.0} million, down IL 1.4 million since last turn"), ui.Q<VisualElement>("meter-treasury").tooltip);
        Assert.AreEqual(Inv($"Foreign currency: ${now.FxReserves:0.0} million, up $2.0 million since last turn"), ui.Q<VisualElement>("meter-fx").tooltip);
        Assert.AreEqual($"Security: {ui.Q<Label>("security-value").text}, up 5 points since last turn", ui.Q<VisualElement>("meter-security").tooltip);
        Assert.AreEqual($"International support: {ui.Q<Label>("standing-value").text}, down 1 point since last turn", ui.Q<VisualElement>("meter-standing").tooltip);
        // Immigration after one turn has no change yet (it needs two turns of arrivals): the caption and the count only.
        Assert.AreEqual($"Immigration: {ui.Q<Label>("immigration-value").text}", ui.Q<VisualElement>("meter-immigration").tooltip);
        // Every tooltip agrees with the meter's shown change: "up" with a gain, "down" with a loss, neither without one.
        foreach (var id in MeterIds)
        {
            var tip = ui.Q<VisualElement>("meter-" + id).tooltip; var delta = ui.Q<Label>(id + "-delta");
            StringAssert.StartsWith(Text["hud.caption." + id] + ": ", tip, id);
            Assert.AreEqual(delta.ClassListContains("delta-up"), tip.Contains(", up "), id + ": " + tip);
            Assert.AreEqual(delta.ClassListContains("delta-down"), tip.Contains(", down "), id + ": " + tip);
        }
    }

    /// <summary>#398 review S3: the spoken captions are strings (so a Hebrew UI says Hebrew captions), and the English ones
    /// are the captions the bar shows, so the two cannot drift apart. #193: in each language the bar's visible caption is
    /// the caption its tooltip says (HudPresenter sets both from hud.caption.*), so a Hebrew bar no longer shows "Treasury"
    /// over a tooltip that says "אוצר".</summary>
    [Test]
    public void TheSpokenCaptionsAreTheBarsCaptions()
    {
        using var ui = new UiTestKit();
        foreach (var id in MeterIds)
            Assert.AreEqual(ui.Q<VisualElement>("meter-" + id).Q<Label>(className: "meter-name").text, Text["hud.caption." + id], id);
        foreach (var (language, text) in new[] { ("he", He), ("en", Text) })
        {
            using var hud = new HudPresenter(ui.Root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text), text);
            foreach (var id in MeterIds)
            {
                var meter = ui.Q<VisualElement>("meter-" + id);
                var shown = meter.Q<Label>(className: "meter-name").text;
                Assert.AreEqual(text["hud.caption." + id], shown, $"{language}: {id}");
                StringAssert.StartsWith(shown + ":", meter.tooltip, $"{language}: {id}'s tooltip says the caption it shows");
            }
        }
    }

    /// <summary>#398: a negative net position, a people count and a Hebrew UI, each through the real path (the
    /// presenter with the UI's own strings). Hebrew uses neutral words for a rise and a fall: on Immigration "עלייה"
    /// and "ירידה" would read as aliyah and emigration (review S2).</summary>
    [Test]
    public void TheSpokenTextCoversNegativeMoneyCountsAndHebrew()
    {
        var bridge = NewBridge();
        PlayUntilImmigrationChanges(bridge);
        var m = HudModel.From(bridge, Text);
        Assert.AreEqual(m.Immigration.Value, m.Immigration.SayValue, "a count is said as shown");
        Assert.AreEqual(m.Immigration.Delta.Substring(1), m.Immigration.SayChange, "a count's change is said without its sign");
        var prev = bridge.Session.Previous!; var now = bridge.State;
        now.Debt = now.Treasury + 3;                                         // net -3.0
        prev.Debt = prev.Treasury - 1;                                       // net +1.0 before: down by 4
        m = HudModel.From(bridge, Text);
        Assert.AreEqual("Money: -IL 3.0 million, down IL 4.0 million since last turn", HudModel.Spoken(Text, "Money", m.Treasury));
        using var ui = new UiTestKit();
        var he = He;
        new HudPresenter(ui.Root, bridge, he);
        var said = ui.Q<VisualElement>("meter-treasury").tooltip;
        // The values carry left-to-right marks (U+200E, #543), so the minus stays on the left of "IL" in a right-to-left line.
        Assert.AreEqual("כסף: \u200E-IL \u200E\u200E3.0\u200E מיליון, קיטון של \u200EIL \u200E\u200E4.0\u200E מיליון מאז התור הקודם", said);
        Assert.AreEqual("כסף: -IL 3.0 מיליון, קיטון של IL 4.0 מיליון מאז התור הקודם", said.Replace("\u200E", ""));
        foreach (var id in MeterIds)
        {
            var tip = ui.Q<VisualElement>("meter-" + id).tooltip;
            StringAssert.StartsWith(he["hud.caption." + id] + ": ", tip, id);
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(tip, "[A-Za-z]{3,}"), $"{id}: English words in the Hebrew text: {tip}");
            Assert.IsFalse(tip.Contains("עלייה של") || tip.Contains("ירידה של"), $"{id}: {tip}");
        }
    }

    /// <summary>#398 review S1: a resumed game shows the changes the player saw before the reload, on all six meters.
    /// GameSession.Load replays the save's own commands and checks the hash, so the replay's last two steps are the
    /// player's; Immigration needs the step before the last, which the session now keeps. Both ways a save arrives: at
    /// start (a new bridge) and through Replace.</summary>
    [Test]
    public void AResumedGameShowsTheChangesOfTheSessionThatSavedIt()
    {
        var played = NewBridge();
        PlayUntilImmigrationChanges(played);
        using var saved = new UiTestKit();
        new HudPresenter(saved.Root, played, Text);
        var saveText = played.Session.Save.Write();
        foreach (var how in new[] { "start", "replace" })
        {
            using var ui = new UiTestKit();
            var loaded = GameSession.Load(C, saveText, out var warnings);
            Assert.IsEmpty(warnings, how);
            SimBridge bridge;
            if (how == "start") bridge = new SimBridge(loaded, Text);
            else { bridge = NewBridge(); bridge.EndTurn(); bridge.EndTurn(); bridge.Replace(loaded); }
            new HudPresenter(ui.Root, bridge, Text);
            Assert.AreEqual(played.State.Turn, bridge.State.Turn, how);
            foreach (var id in MeterIds)
            {
                var before = saved.Q<Label>(id + "-delta"); var after = ui.Q<Label>(id + "-delta");
                Assert.AreEqual(before.text, after.text, $"{how}: {id}");
                Assert.AreEqual(before.ClassListContains("delta-up"), after.ClassListContains("delta-up"), $"{how}: {id}");
                Assert.AreEqual(before.ClassListContains("delta-down"), after.ClassListContains("delta-down"), $"{how}: {id}");
                Assert.AreEqual(saved.Q<VisualElement>("meter-" + id).tooltip, ui.Q<VisualElement>("meter-" + id).tooltip, $"{how}: {id}");
            }
            Assert.AreNotEqual("", ui.Q<Label>("immigration-delta").text, how + ": Immigration's change survives the reload");
        }
    }
}
