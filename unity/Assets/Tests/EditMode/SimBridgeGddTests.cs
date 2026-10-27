#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The GDD orders through the UI's queue (#380): AssignWorkers, Train and CancelTraining queue in the slice
/// (the real content, the GDD game since the cutover), an id the content or state does not have is refused before End Turn, an order replaces an earlier one
/// with the same key, and the legacy loop's content queues none of them.</summary>
public class SimBridgeGddTests
{
    static readonly Content Slice = ContentLoader.Load(Paths.Content);
    /// <summary>The legacy loop: the real content without eras.yaml's loop switch (the real game runs gdd since the cutover).</summary>
    internal static readonly Content Legacy = LoadLegacy();

    /// <summary>A fresh copy of the legacy content, for a test that changes it.</summary>
    internal static Content LoadLegacy()
    {
        const string loop = "    systems: { loop: gdd }\n";
        var dir = Paths.Content;
        var events = System.IO.Directory.GetFiles(System.IO.Path.Combine(dir, "events"), "*.yaml").Select(f => "events/" + System.IO.Path.GetFileName(f)).ToList();
        Assert.IsTrue(System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "eras.yaml")).Contains(loop), "eras.yaml has the switch to take out");
        return ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(dir, rel);
            if (!System.IO.File.Exists(path)) return null;
            var text = System.IO.File.ReadAllText(path);
            return rel == "eras.yaml" ? text.Replace(loop, "") : text;
        }, events);
    }
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge Bridge(Content c) => new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text);

    /// <summary>R-TURN-004: a Move given through the bridge shows at once (State), and End turn keeps it.</summary>
    [Test]
    public void AMoveShowsAtOnce()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Slice, bridge.State);
        var start = (bridge.State.Units[move.Unit].Q, bridge.State.Units[move.Unit].R);
        IReadOnlyList<Cause>? lines = null; bridge.Committed += w => lines = w;
        Assert.IsNull(bridge.Queue(move));
        Assert.AreEqual((move.Q, move.R), (bridge.State.Units[move.Unit].Q, bridge.State.Units[move.Unit].R));
        Assert.AreEqual(start, (bridge.TurnStart.Units[move.Unit].Q, bridge.TurnStart.Units[move.Unit].R));
        Assert.IsEmpty(bridge.Queued, "a committed order is not queued");
        Assert.IsTrue(lines!.Any(w => w.Key == WhyKeys.UnitMoved));
        bridge.EndTurn();
        Assert.AreEqual(GoldenRun.PlaySmokeOrders(Slice).Hash(), bridge.TurnStart.Hash());
    }

    /// <summary>No undo: a committed move is not withdrawn; a second move for the same unit is kept beside it.</summary>
    [Test]
    public void ACommittedMoveCannotBeWithdrawn()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Slice, bridge.State);
        bridge.Queue(move);
        bridge.Withdraw(move);
        Assert.AreEqual(1, bridge.Session.Committed.Count);
    }

    /// <summary>Spec 3.2: Send home stays deferred, and a unit with it queued takes no more moves or orders.</summary>
    [Test]
    public void AUnitWithSendHomeQueuedTakesNoMoreOrders()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Slice, bridge.State);
        Assert.IsNull(bridge.Queue(new UnitOrder(move.Unit, Demobilization.SendHome)));
        Assert.AreEqual(1, bridge.Queued.Count, "deferred, as today");
        Assert.AreEqual(Text["order.going_home"], bridge.Queue(move));
        Assert.IsEmpty(bridge.Session.Committed);
    }

    /// <summary>Codex's review of #1527, finding 3: Send home queued first, a deferred order for the unit (Reinforce, the
    /// one a soldier can take) is refused like an instant one, and only Send home stays queued.</summary>
    [Test]
    public void SendHomeThenReinforceRefusesTheReinforcement()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var unit = bridge.NeedsOrders[0];
        var home = new UnitOrder(unit, Demobilization.SendHome);
        Assert.IsNull(bridge.Queue(home));
        Assert.AreEqual(Text["order.going_home"], bridge.Queue(new Reinforce(unit, "tel_aviv", 1)));
        CollectionAssert.AreEqual(new Command[] { home }, bridge.Queued.ToList());
    }

    /// <summary>Codex's review of #1527, finding 3, the other order: Reinforce queued first, Send home takes the unit's
    /// place in the queue. The reinforcement is withdrawn and its Attention refunded, so End turn never trains replacements
    /// for a unit that goes home in the same turn.</summary>
    [Test]
    public void ReinforceThenSendHomeWithdrawsTheReinforcement()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var unit = bridge.NeedsOrders[0]; var other = bridge.NeedsOrders[1];
        var start = bridge.AttentionLeft!.Value;
        var kept = new Reinforce(other, "tel_aviv", 1);
        Assert.IsNull(bridge.Queue(new Reinforce(unit, "tel_aviv", 1)));
        Assert.IsNull(bridge.Queue(kept));
        var home = new UnitOrder(unit, Demobilization.SendHome);
        Assert.IsNull(bridge.Queue(home));
        CollectionAssert.AreEqual(new Command[] { kept, home }, bridge.Queued.ToList(), "the other unit's order stays");
        Assert.AreEqual(start - bridge.Price(kept)!.Value - bridge.Price(home)!.Value, bridge.AttentionLeft!.Value, 1e-9, "the withdrawn order is refunded");
    }

    [Test]
    public void SkippedUnitsAndUnitsGoingHomeDoNotNeedOrders()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var first = bridge.NeedsOrders[0]; var second = bridge.NeedsOrders[1];
        bridge.Skip(first);
        bridge.Queue(new UnitOrder(second, Demobilization.SendHome));
        CollectionAssert.DoesNotContain(bridge.NeedsOrders.ToList(), first);
        CollectionAssert.DoesNotContain(bridge.NeedsOrders.ToList(), second);
        bridge.EndTurn();
        Assert.IsFalse(bridge.IsSkipped(first), "a skip ends with the turn");
    }

    /// <summary>A slice bridge after one turn that opened a halutz cohort, and that cohort's id.</summary>
    static (SimBridge bridge, int cohort) WithACohort()
    {
        var b = Bridge(Slice);
        Assert.IsNull(b.Queue(new Train("tel_aviv", "halutz", 1)));
        b.EndTurn();
        Assert.AreEqual(1, b.State.Cohorts.Count, "the turn opened one cohort");
        return (b, b.State.Cohorts.Keys.First());
    }

    [Test]
    public void EachGddOrderQueuesInTheSlice()
    {
        var b = Bridge(Slice);
        Assert.IsNull(b.Queue(new AssignWorkers("tel_aviv", "job", 90)));
        Assert.IsNull(b.Queue(new Train("tel_aviv", "recruit", 1)));
        Assert.AreEqual(2, b.Queued.Count);

        var (withCohort, id) = WithACohort();
        Assert.IsNull(withCohort.Queue(new CancelTraining(id)));
        Assert.AreEqual(1, withCohort.Queued.Count);
    }

    [Test]
    public void AnUnknownIdIsRefusedAndNothingIsQueued()
    {
        var t = Text;
        var b = Bridge(Slice);
        // jaffa is a 1948 settlement but not a GDD city (its own map row; Tel Aviv-Yafo is one city, GDD Phase 3 PR 1).
        Assert.AreEqual(t.Format("order.unknown", "city"), b.Queue(new AssignWorkers("jaffa", "job", 1)));
        Assert.AreEqual(t.Format("order.unknown", "city"), b.Queue(new Train("jaffa", "halutz", 1)));
        Assert.AreEqual(t.Format("order.unknown", "training"), b.Queue(new Train("tel_aviv", "pilot", 1)));
        // civilian is a unit type, but nobody trains to it: it has no facility.
        Assert.AreEqual(t.Format("order.unknown", "training"), b.Queue(new Train("tel_aviv", "civilian", 1)));
        Assert.AreEqual(t.Format("order.unknown", "cohort"), b.Queue(new CancelTraining(7)));
        Assert.AreEqual(0, b.Queued.Count);
    }

    [Test]
    public void AnOrderWithTheSameKeyReplacesTheEarlierOne()
    {
        var b = Bridge(Slice);
        b.Queue(new AssignWorkers("tel_aviv", "job", 10));
        b.Queue(new AssignWorkers("tel_aviv", "job", 20));
        Assert.AreEqual(1, b.Queued.Count);
        Assert.AreEqual(20, ((AssignWorkers)b.Queued[0]).Count);

        // A fresh bridge, so the Attention the work order took cannot refuse a training order.
        var t = Bridge(Slice);
        Assert.IsNull(t.Queue(new Train("tel_aviv", "halutz", 1)));
        Assert.IsNull(t.Queue(new Train("tel_aviv", "halutz", 2)));
        Assert.AreEqual(1, t.Queued.Count);
        Assert.AreEqual(2, ((Train)t.Queued[0]).Count);
        Assert.IsNull(t.QueuedLike(new Train("tel_aviv", "recruit", 1)), "another track is its own order");
        Assert.IsNull(t.QueuedLike(new AssignWorkers("tel_aviv", "job", 1)), "work and training orders do not share a key");

        var (withCohort, id) = WithACohort();
        withCohort.Queue(new CancelTraining(id));
        withCohort.Queue(new CancelTraining(id));
        Assert.AreEqual(1, withCohort.Queued.Count);
    }

    /// <summary>The work key names the city: another city's order is its own, not a replacement. (QueuedLike checks keys
    /// only, so a city the slice does not have is enough.) One per city is right while "job" is the only role; craftsmen
    /// (Phase 3, D-D4) will need the role in the key.</summary>
    [Test]
    public void AnotherCitysWorkOrderIsItsOwn()
    {
        var b = Bridge(Slice);
        Assert.IsNull(b.Queue(new AssignWorkers("tel_aviv", "job", 10)));
        Assert.IsNull(b.QueuedLike(new AssignWorkers("jerusalem", "job", 1)), "another city is its own order");
    }

    /// <summary>The cancel key names the cohort: cancelling two cohorts in one turn keeps both orders.</summary>
    [Test]
    public void AnotherCohortsCancelIsItsOwn()
    {
        var (b, id) = WithACohort();
        Assert.IsNull(b.Queue(new CancelTraining(id)));
        Assert.IsNull(b.QueuedLike(new CancelTraining(id + 1)), "another cohort is its own order");
    }

    [Test]
    public void TheLegacyLoopQueuesNoGddOrder()
    {
        var t = Text;
        var b = Bridge(Legacy);
        Assert.IsFalse(Legacy.IsGdd);
        Assert.AreEqual(t.Format("order.unknown", "city"), b.Queue(new AssignWorkers("tel_aviv", "job", 1)));
        Assert.AreEqual(t.Format("order.unknown", "city"), b.Queue(new Train("tel_aviv", "halutz", 1)));
        Assert.AreEqual(t.Format("order.unknown", "cohort"), b.Queue(new CancelTraining(1)));
        Assert.AreEqual(0, b.Queued.Count);
    }
}

/// <summary>#1688: absence of Attention in gdd-0.3, with the old accounting retained in older rulesets.</summary>
public class NoAttentionPresentationTests
{
    static Content Load(bool modern)
    {
        var dir = Paths.Content;
        return ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(dir, rel);
            if (!System.IO.File.Exists(path)) return null;
            var value = System.IO.File.ReadAllText(path);
            return modern && rel == "eras.yaml" ? value.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : value;
        }, System.IO.Directory.GetFiles(System.IO.Path.Combine(dir, "events"), "*.yaml").Select(f => "events/" + System.IO.Path.GetFileName(f)));
    }
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static SimBridge Bridge(bool modern, bool he = false)
    {
        var c = Load(modern);
        var b = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text(he));
        if (modern)
        {
            // Any accidental obsolete price read must fail, rather than silently reading a plausible zero.
            foreach (var r in c.EraRules.Values.Append(c.Rules))
            { r.AttentionCost.Clear(); r.AttentionPerTurn = double.NaN; r.RungAttentionStep = double.NaN; }
            b.State.Attention = double.NaN;
        }
        return b;
    }

    [Test]
    public void BridgeHasNoPoolOrPriceAndStillReplacesAndWithdrawsOrders()
    {
        var b = Bridge(true);
        var first = new SetPolicy("rationing", .5);
        var next = new SetPolicy("rationing", .7);
        Assert.IsNull(b.AttentionLeft);
        Assert.IsNull(b.Price(first));
        Assert.IsTrue(b.CanPay(first));
        Assert.IsNull(b.Queue(first)); Assert.IsNull(b.Queue(next));
        Assert.AreEqual(1, b.Queued.Count);
        Assert.AreEqual(next, b.Queued[0]);
        b.Withdraw(next); Assert.IsEmpty(b.Queued);
        Assert.IsNotNull(b.Queue(new SetPolicy("not-a-policy", .5)), "real validity checks remain");
    }

    [TestCase(false)] [TestCase(true)]
    public void HudHasNoAttentionTextOrHitTarget(bool he)
    {
        var b = Bridge(true, he);
        Assert.AreEqual("", HudModel.From(b, Text(he)).Attention);
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, Text(he));
        var line = ui.Q<UnityEngine.UIElements.Label>("attention");
        Assert.AreEqual("", line.text);
        Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.None, line.style.display.value);
        Assert.AreEqual(UnityEngine.UIElements.PickingMode.Ignore, line.pickingMode);
        Assert.IsFalse(line.focusable);
        Assert.IsTrue(string.IsNullOrEmpty(line.tooltip));
        foreach (var label in ui.Q<VisualElement>("top-bar").Query<Label>().ToList())
        {
            bool visible = true;
            for (VisualElement? e = label; e != null; e = e.parent)
                if (e.style.display.value == DisplayStyle.None) visible = false;
            if (!visible) continue;
            StringAssert.DoesNotContain(Text(he)["capture.caption.attention"], label.text ?? "");
            StringAssert.DoesNotContain(Text(he)["capture.caption.attention"], label.tooltip ?? "");
        }
        StringAssert.DoesNotContain("NaN", ui.Q<UnityEngine.UIElements.Button>("end-turn").text);
    }

    [TestCase(false)] [TestCase(true)]
    public void NullCapturePreviewOmitsOnlyAttention(bool he)
    {
        var b = Bridge(true, he); var t = Text(he);
        var preview = new CapturePreview(null, 3, 0, 2, 4, 0, 0, 0);
        var line = CaptureDialogPresenter.PreviewText(b.Content, b.State, preview, t);
        StringAssert.DoesNotContain(t["capture.caption.attention"], line);
        StringAssert.Contains(t["hud.caption.treasury"], line);
        StringAssert.Contains(t["capture.caption.food"], line);
        StringAssert.Contains(t["capture.caption.arms"], line);
    }

    [TestCase(false)] [TestCase(true)]
    public void EventsRemainAnswerableWithoutAttentionOrDelegationWarning(bool he)
    {
        var b = Bridge(true, he); var t = Text(he);
        b.Content.Events.Add(new GameEvent { Id = "no-attention-card", Title = "A decision", Text = "Decide", Options = new List<EventOption> { new EventOption { Id = "yes", Text = "Continue" } } });
        b.State.PendingEvents.Clear(); b.State.PendingEvents.Add("no-attention-card");
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, b, t, null);
        var option = ui.Q<UnityEngine.UIElements.Button>("option-yes");
        Assert.AreEqual("Continue", option.text); Assert.IsTrue(option.enabledSelf);
        Assert.IsFalse(ui.Q<UnityEngine.UIElements.Button>("end-turn").ClassListContains(EventCardPresenter.EndTurnWaitingClass));
        Assert.IsNull(b.Queue(new ChooseEventOption("no-attention-card", "yes")));
        Assert.IsNull(card.Shown, "the answer is queued, so the card closes before End turn");
        Assert.IsNotNull(b.QueuedLike(new ChooseEventOption("no-attention-card", "")));
    }

    [TestCase(false)] [TestCase(true)]
    public void WorkersKeepTheirEndTurnTimingWithoutAnAttentionCost(bool he)
    {
        var b = Bridge(true, he); var t = Text(he);
        var spot = b.Content.SettlementHexes.First(x => b.State.Cities.ContainsKey(x.Id));
        var host = new UnityEngine.UIElements.VisualElement();
        GddHexPanel.Render(host, new UnityEngine.UIElements.VisualElement(), b, t, spot.Q, spot.R, id => id);
        var line = host.Q<UnityEngine.UIElements.Label>("city-workers-cost");
        Assert.IsNotNull(line);
        StringAssert.DoesNotContain(t["capture.caption.attention"], line.text);
        StringAssert.StartsWith(t["city.workers_ordered"].Split('{')[0], line.text);
    }

    [TestCase(false)] [TestCase(true)]
    public void SmokeAnswersAnEventWithoutReadingTheObsoletePool(bool he)
    {
        var b = Bridge(true, he); var t = Text(he);
        b.Content.Events.Add(new GameEvent { Id = "smoke-no-attention", Title = "Decision", Text = "Decide", Options = new List<EventOption> { new EventOption { Id = "yes", Text = "Continue" } } });
        b.State.PendingEvents.Clear(); b.State.PendingEvents.Add("smoke-no-attention");
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, b, t, null);
        var logs = new List<string>();
        var smoke = GameRoot.Smoke(ui.Root, b, card, 1, logs.Add);
        try
        {
            Assert.IsTrue(smoke.MoveNext());
            Assert.IsTrue(smoke.MoveNext());
            Assert.IsNull(card.Shown);
            Assert.IsEmpty(logs.Where(x => x.StartsWith("SMOKE FAIL")));
        }
        finally { (smoke as System.IDisposable)?.Dispose(); }
    }

    [TestCase(false)] [TestCase(true)]
    public void DefenceAndCabinetStillRenderTheirActualRequirements(bool he)
    {
        var b = Bridge(true, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var defence = new DefencePresenter(ui.Root, b, t);
        using var cabinet = new CabinetPresenter(ui.Root, b, t, null);
        var proposed = defence.ProposedMobilization;
        Assert.AreEqual(t.Format("defence.mob_cost", PlayerText.Money(b.Content, "treasury", EconomySystem.MobilizationCost(b.Content, b.State.Date, proposed))), ui.Q<Label>("mob-cost").text);
        var refusedAttention = 0; b.RefusedForAttention += () => refusedAttention++;
        var shares = new Dictionary<string, double>(b.State.Budget);
        Assert.IsNull(b.Queue(new AllocateBudget(shares)));
        var key = shares.Keys.First(); var original = shares[key]; shares[key] = original + .1;
        var retained = (AllocateBudget)b.QueuedLike(new AllocateBudget(shares))!;
        Assert.AreEqual(original, retained.Shares[key], "budget remains a snapshot");
        b.Withdraw(retained);
        Assert.AreEqual(0, refusedAttention);
        b.State.GameOver = "finished";
        Assert.AreEqual(t["order.game_over"], b.Queue(new SetPolicy("rationing", .5)));
    }

    [Test]
    public void OldRulesetsKeepTheirAttentionPriceAndCaptureLine()
    {
        foreach (var c in new[] { Load(false), SimBridgeGddTests.LoadLegacy() })
        {
            var t = Text(false); var b = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), t);
            Assert.IsTrue(c.UsesAttention);
            Assert.Greater(b.Price(new SetPolicy("rationing", .5)), 0);
            Assert.IsNotEmpty(HudModel.From(b, t).Attention);
            StringAssert.Contains(t["capture.caption.attention"], CaptureDialogPresenter.PreviewText(c, b.State, new CapturePreview(-1, 0, 0, 0, 0, 0, 0, 0), t));
        }
    }
}
