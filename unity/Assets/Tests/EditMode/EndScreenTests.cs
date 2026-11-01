#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The end screen (#812; D-H4): once the game is over it shows the sim's end report (EndReport.For, #810), formatted
/// only, over the real final state; closing, rebuilding (the language switch) and New Game's first press keep that state and
/// the saved bytes, and only New Game's second press starts over.</summary>
public class EndScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>The HUD, the toast, New Game, the autosave and the end screen on Game.uxml, over a fresh Historical game.</summary>
    sealed class Kit : System.IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge; public readonly SaveStore Store; public readonly NewGamePresenter NewGame; public readonly TurnReportPresenter Report;
        public EndScreenPresenter End;
        readonly List<System.IDisposable> owned = new List<System.IDisposable>();
        readonly string dir = Path.Combine(Path.GetTempPath(), "statecraft-end-" + System.Guid.NewGuid().ToString("N"));

        public Kit(UiText text, System.Action<GameState>? setUp = null)
        {
            var session = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            setUp?.Invoke(session.State);
            Bridge = new SimBridge(session, text);
            Store = new SaveStore(dir);
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            var nav = new ScreenNav(Ui.Root); owned.Add(nav);
            owned.Add(Report = new TurnReportPresenter(Ui.Root, Bridge, text, nav));
            var toast = new ToastPresenter(Ui.Root, Bridge, text); owned.Add(toast);
            owned.Add(NewGame = new NewGamePresenter(Ui.Root, Bridge, Store, toast, text, () => GoldenRun.Seed));
            owned.Add(GameStart.Autosave(Bridge, Store, text));
            End = new EndScreenPresenter(Ui.Root, Bridge, text, NewGame.Press);
        }

        /// <summary>The end screen built again over the same game, as Settings' language switch does (GameRoot.SwitchLanguage):
        /// open again only if it was open.</summary>
        public void Rebuild(UiText text)
        {
            bool open = End.IsOpen;
            End.Dispose();
            End = new EndScreenPresenter(Ui.Root, Bridge, text, NewGame.Press, show: open);
        }

        /// <summary>The end screen made again as a load or a restart makes it (GameRoot.Begin with no keep).</summary>
        public void Dispose_End_Reload(UiText text)
        {
            End.Dispose();
            End = new EndScreenPresenter(Ui.Root, Bridge, text, NewGame.Press);
        }

        public VisualElement Q(string name) => Ui.Q<VisualElement>(name);
        public string Line(string name) => Ui.Q<Label>("end-screen-" + name).text;

        public void Dispose()
        {
            End.Dispose();
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Ui.Dispose();
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    /// <summary>Money and Food exhausted and kept so (as the sim's DefeatTests): the debt far over the ceiling, an empty stock
    /// and a foreign currency crisis deep enough to last, so nothing is imported (R-END-001).</summary>
    static void Exhausted(GameState s)
    {
        s.Debt = 1e12;
        s.Food = 0;
        s.FxReserves = -1e12;
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
    }

    /// <summary>A game on its last day, ended as the MVP ends, with the first objective completed a month early.</summary>
    static void Ended(GameState s)
    {
        var first = C.Objectives.Single(o => o.Id == "absorb_immigrants");   // by id: the first in id order, as before R-OBJ-007
        Objectives.Record(C, s, C.RulesAt(s.Date).Mvp!, first.Id, first.ReferenceDate.AddMonths(-1), new List<Cause>());
        s.Date = C.End;
        s.GameOver = GameOverCodes.MvpEnded;
    }

    [Test]
    public void OpensOnGameOverAndADefeatShowsItsCause()
    {
        using var k = new Kit(En, Exhausted);
        Assert.IsFalse(k.End.IsOpen, "no end screen while the game is on");
        for (int turn = 0; turn < 3 && k.Bridge.State.GameOver == null; turn++) k.Bridge.EndTurn();
        Assert.AreEqual(GameOverCodes.DefeatEconomy, k.Bridge.State.GameOver, "three exhausted turn ends are the economic defeat");
        Assert.IsTrue(k.End.IsOpen, "the turn that ends the game opens the end screen");
        Assert.AreEqual(DisplayStyle.Flex, k.Q("end-screen").style.display.value);
        Assert.AreEqual(En["end.outcome.defeat_economy"], k.Line("outcome"));
        Assert.IsFalse(k.Q("top-bar").enabledSelf, "the screen covers the HUD");
        Assert.IsTrue(k.Q("end-screen").enabledSelf, "and takes input itself");
    }

    [Test]
    public void ShowsEveryPartOfTheReportAndNothingElse()
    {
        using var k = new Kit(En, Ended);
        Assert.IsTrue(k.End.IsOpen, "a loaded ended game opens on its end screen");
        var s = k.Bridge.State; var r = EndReport.For(C, s);
        var names = k.Ui.Q<ScrollView>("end-screen-rows").Children().Select(x => x.name).ToList();
        var expected = new[] { "outcome", "score", "score-reference" }.Concat(r.Objectives.Select(o => "objective-" + o.Id))
            .Concat(new[] { "cohesion", "standing", "population" }).Select(n => "end-screen-" + n);
        CollectionAssert.AreEqual(expected, names, "the report's parts, in order");
        Assert.AreEqual(En["end.outcome.mvp_ended"], k.Line("outcome"));
        Assert.AreEqual(En.Format("hud.score", s.Score), k.Line("score"));
        Assert.Greater(s.Score, 0);
        Assert.AreEqual(200, r.ReferenceScore, "the reference is the sim's number (Q-SCORE-2)");
        Assert.AreEqual(En.Format("end.reference_score", r.ReferenceScore), k.Line("score-reference"));
        StringAssert.Contains("200", k.Line("score-reference"));
        // The first objective: the card's title, the player's date and the historical date (Q-END-1).
        var first = C.Objectives.Single(o => o.Id == "absorb_immigrants");   // by id: the first in id order, as before R-OBJ-007
        string Date(System.DateTime d) => En.Format("date.day_month_year", d.Day, En["date.month_" + d.Month], d.Year);
        var title = EventSystem.Title(C, s, C.Events.First(e => e.Id == first.Card));
        Assert.AreEqual(En.Format("end.objective_done", title, Date(first.ReferenceDate.AddMonths(-1)), Date(first.ReferenceDate)), k.Line("objective-" + first.Id));
        foreach (var o in C.Objectives.Where(o => o.Id != first.Id))
            StringAssert.Contains(Date(o.ReferenceDate), k.Line("objective-" + o.Id), "an open objective still shows its historical date");
        Assert.AreEqual(En.Format("end.parameter", En["hud.caption.cohesion"], PlayerText.Percent(r.SocialCohesion)), k.Line("cohesion"));
        Assert.AreEqual(En.Format("end.parameter", En["hud.caption.standing"], PlayerText.Percent(r.InternationalSupport)), k.Line("standing"));
        // The one comparison with history: the population beside its CBS anchor (Q-END-2, R-END-006).
        var anchor = r.PopulationHistory!;
        Assert.AreEqual(En.Format("end.population_history", r.Population.ToString("N0", System.Globalization.CultureInfo.InvariantCulture),
            Date(anchor.Date), anchor.Jews.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)), k.Line("population"));
    }

    /// <summary>A cause's history note (Q-DEF-4 (b)) sits right under its line when the strings carry one, and nowhere else.
    /// The armistice note is held out of the strings until the person's check is recorded, so a fixture string stands in.</summary>
    [Test]
    public void AWarLossShowsItsHistoryNoteUnderItsLineAndOtherOutcomesShowNone()
    {
        var withNote = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text + "\nend.note.defeat_war: \"Fixture note.\"\n");
        using var k = new Kit(withNote, s => { s.Date = C.End; s.GameOver = GameOverCodes.DefeatWar; });
        var names = k.Ui.Q<ScrollView>("end-screen-rows").Children().Select(x => x.name).ToList();
        CollectionAssert.AreEqual(new[] { "end-screen-outcome", "end-screen-note", "end-screen-score" }, names.Take(3), "the note follows the outcome line");
        Assert.AreEqual("Fixture note.", k.Line("note"));
        Assert.IsTrue(k.Ui.Q<Label>("end-screen-note").ClassListContains("end-screen-note"));
        using var ended = new Kit(withNote, Ended);
        Assert.IsFalse(ended.Ui.Q<ScrollView>("end-screen-rows").Children().Any(x => x.name == "end-screen-note"), "no note for the MVP end");
    }

    /// <summary>A regression guard (it passes without the note change too): with no note in the strings (the shipped EN and HE
    /// files, until the person's check), a war loss goes straight from its line to the score: no note row, no empty gap.
    /// Adding the note string back means updating this test too.</summary>
    [Test]
    public void AWarLossWithNoNoteStringGoesStraightFromItsLineToTheScore()
    {
        foreach (var text in new[] { En, He })
        {
            using var k = new Kit(text, s => { s.Date = C.End; s.GameOver = GameOverCodes.DefeatWar; });
            var names = k.Ui.Q<ScrollView>("end-screen-rows").Children().Select(x => x.name).ToList();
            CollectionAssert.AreEqual(new[] { "end-screen-outcome", "end-screen-score" }, names.Take(2), "the score follows the outcome line");
            CollectionAssert.DoesNotContain(names, "end-screen-note");
        }
    }

    /// <summary>A note follows only its own cause's line: an outcome with no line of its own (a legacy code, shown as the
    /// generic game-over line) gets no note even when the strings carry one for it.</summary>
    [Test]
    public void ANoteNeverFollowsTheGenericGameOverLine()
    {
        var withNote = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text + "\nend.note.coalition_collapse: \"Fixture note.\"\n");
        Assert.IsFalse(withNote.Has("end.outcome." + GameOverCodes.CoalitionCollapse), "precondition: the legacy code has no outcome line");
        using var k = new Kit(withNote, s => { s.Date = C.End; s.GameOver = GameOverCodes.CoalitionCollapse; });
        Assert.AreEqual(withNote["order.game_over"], k.Line("outcome"), "precondition: the generic line shows");
        var names = k.Ui.Q<ScrollView>("end-screen-rows").Children().Select(x => x.name).ToList();
        CollectionAssert.AreEqual(new[] { "end-screen-outcome", "end-screen-score" }, names.Take(2), "the score follows the generic line");
        CollectionAssert.DoesNotContain(names, "end-screen-note");
    }

    // Q-END-3: the prisoners returned, worded as Defence words them, only when some went home (no zero row).
    [Test]
    public void ShowsThePrisonersReturnedOnlyWhenSomeWentHome()
    {
        using (var none = new Kit(En, Ended))
            CollectionAssert.DoesNotContain(none.Ui.Q<ScrollView>("end-screen-rows").Children().Select(x => x.name), "end-screen-prisoners");
        using var k = new Kit(En, s => { Ended(s); s.PrisonersReturned["lebanon"] = 4; });
        Assert.AreEqual("Prisoners returned: 4,000", k.Line("prisoners"));
        Assert.AreEqual("שבויים שהוחזרו: \u200e4,000\u200e", EndScreenPresenter.Lines(C, k.Bridge.State, He).Single(l => l.Name == "prisoners").Text);
        // The same count as the Defence screen shows over the same state.
        k.Q("screen-defence").style.display = DisplayStyle.Flex;   // open, as ScreenNav opens it (a hidden screen does not re-render)
        using (new DefencePresenter(k.Ui.Root, k.Bridge, En))
            Assert.AreEqual(k.Ui.Q<Label>("prisoners-returned").text, k.Line("prisoners"));
    }

    [Test]
    public void CloseRebuildAndTheFirstNewGamePressKeepTheStateAndTheSave()
    {
        using var k = new Kit(En, Exhausted);
        for (int turn = 0; turn < 3 && k.Bridge.State.GameOver == null; turn++) k.Bridge.EndTurn();
        Assert.IsTrue(k.End.IsOpen);
        var hash = k.Bridge.State.Hash();
        var saved = File.ReadAllBytes(k.Store.AutosavePath);

        k.Report.Close();   // the last turn's report, under the end screen
        UiTestKit.Press(k.Ui.Q<Button>("end-screen-close"));
        Assert.IsFalse(k.End.IsOpen, "Close leaves the map to inspect");
        Assert.IsTrue(k.Q("top-bar").enabledSelf, "the HUD is back");
        Assert.AreEqual(hash, k.Bridge.State.Hash(), "closing changes nothing");
        CollectionAssert.AreEqual(saved, File.ReadAllBytes(k.Store.AutosavePath), "nor the saved game");
        Assert.IsFalse(k.Ui.Q<Button>("end-turn").enabledSelf, "End Turn stays disabled after the end");
        Assert.AreEqual(En["order.game_over"], k.Bridge.Queue(new BuyArms(1)), "the map is read-only: an order is refused");

        k.Rebuild(He);   // the language switch builds the screens again over the same game
        Assert.IsFalse(k.End.IsOpen, "a screen the player closed stays closed in the other language");
        Assert.IsTrue(k.Q("top-bar").enabledSelf);
        k.Dispose_End_Reload(He);
        Assert.IsTrue(k.End.IsOpen, "a loaded ended game opens on its end screen again");
        Assert.AreEqual(He["end.title"], k.Ui.Q<Label>("end-screen-title").text);
        Assert.AreEqual(He["end.outcome.defeat_economy"], k.Line("outcome"));

        UiTestKit.Press(k.Ui.Q<Button>("end-screen-new-game"));
        Assert.IsTrue(k.NewGame.Armed, "the first press only asks");
        Assert.IsTrue(k.End.IsOpen);
        Assert.AreEqual(hash, k.Bridge.State.Hash(), "showing, closing and rebuilding the screen change nothing");
        CollectionAssert.AreEqual(saved, File.ReadAllBytes(k.Store.AutosavePath), "nor the saved game");

        UiTestKit.Press(k.Ui.Q<Button>("end-screen-new-game"));
        Assert.IsNull(k.Bridge.State.GameOver, "the second press starts a new game");
        Assert.IsFalse(k.End.IsOpen, "which has no end screen");
        Assert.IsFalse(File.Exists(k.Store.AutosavePath), "New Game removes the old save, as it always has");
    }

    [Test]
    public void CancelClosesItAndClosedItTakesNoInput()
    {
        using var k = new Kit(En, Ended);
        var close = k.Ui.Q<Button>("end-screen-close");
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = close; close.SendEvent(e); }
        Assert.IsFalse(k.End.IsOpen, "Esc or a gamepad's Cancel closes it");
        Assert.IsFalse(k.Q("end-screen").enabledSelf, "a closed screen takes no input");
        Assert.IsFalse(k.End.Close(), "closing it again does nothing");
    }

    [Test]
    public void TheLastTurnsReportWaitsUnderTheEndScreenAndADisposedScreenStaysShut()
    {
        using var k = new Kit(En, Exhausted);
        for (int turn = 0; turn < 3 && k.Bridge.State.GameOver == null; turn++) k.Bridge.EndTurn();
        Assert.IsTrue(k.End.IsOpen && k.Report.IsOpen, "the last turn brings both");
        Assert.IsFalse(k.Q("turn-report").enabledSelf, "the report takes no input under the end screen");
        Assert.IsTrue(k.Q("end-screen").enabledSelf);
        UiTestKit.Press(k.Ui.Q<Button>("end-screen-close"));
        Assert.IsTrue(k.Q("turn-report").enabledSelf, "and takes it again when the end screen closes");
        Assert.IsFalse(k.Q("top-bar").enabledSelf, "the report still covers the HUD");
        k.Report.Close();
        k.End.Dispose();
        var again = GameSession.New(C, GoldenRun.Seed, Mode.Historical); Exhausted(again.State);
        k.Bridge.Replace(again);
        for (int turn = 0; turn < 3 && k.Bridge.State.GameOver == null; turn++) k.Bridge.EndTurn();
        Assert.IsNotNull(k.Bridge.State.GameOver);
        Assert.AreEqual(DisplayStyle.None, k.Q("end-screen").style.display.value, "a disposed end screen listens to no turn");
        k.Dispose_End_Reload(En);   // the kit disposes it once more at the end
    }

    /// <summary>#2041: in Hebrew an objective's line titles it as its decision card does - the Hebrew title_he where
    /// there is no variant title, the English otherwise (EventCardPresenter's pick). English is unchanged, pinned by
    /// ShowsEveryPartOfTheReportAndNothingElse. Red-first: Lines used the English EventSystem.Title in every language.
    /// </summary>
    [Test]
    public void HebrewTitlesTheObjectivesAsTheirCardsDo()
    {
        using var k = new Kit(He, Ended);
        var s = k.Bridge.State;
        var lines = EndScreenPresenter.Lines(C, s, He);
        foreach (var o in C.Objectives)
        {
            var card = C.Events.First(e => e.Id == o.Card);
            var variant = Timeline.Variant(C, s, card);
            Assert.IsTrue(variant == null || variant.Title == "", o.Id + " precondition: no variant title");
            Assert.AreNotEqual("", card.TitleHe, o.Id + " precondition: the card has a Hebrew title");
            var line = lines.Single(l => l.Name == "objective-" + o.Id).Text;
            StringAssert.Contains(card.TitleHe, line, o.Id + ": the Hebrew line shows the card's Hebrew title");
            StringAssert.DoesNotContain(EventSystem.Title(C, s, card), line, o.Id + ": not the English title");
        }
    }

    [Test]
    public void HebrewShowsTheHebrewWords()
    {
        using var k = new Kit(He, Ended);
        Assert.AreEqual(He["end.title"], k.Ui.Q<Label>("end-screen-title").text);
        Assert.AreEqual(He["end.outcome.mvp_ended"], k.Line("outcome"));
        Assert.AreEqual(He["report.close"], k.Ui.Q<Button>("end-screen-close").text);
        Assert.AreEqual(He["hud.new_game"], k.Ui.Q<Button>("end-screen-new-game").text);
        StringAssert.StartsWith(He["hud.caption.cohesion"] + ":", k.Line("cohesion"));
    }

    /// <summary>#2060: in Hebrew each date an end-screen line inserts sits between RLE and PDF (U+202B, U+202C), so it
    /// reads as one unit right after "ב־" ("20 בספטמבר 1949") even in a line laid out left to right, instead of the bidi
    /// rules pulling its day onto the next word and its year to the line's far end. The lines are compared ordinally, so
    /// every direction mark counts. The titles are read back from the lines: #2041 decides which title shows, this test
    /// pins the dates around it.</summary>
    [Test]
    public void HebrewIsolatesEachDateItInserts()
    {
        using var k = new Kit(He, Ended);
        var first = C.Objectives.Single(o => o.Id == "absorb_immigrants");
        const char Lrm = '\u200E', Rle = '\u202B', Pdf = '\u202C';
        string Date(System.DateTime d) => $"{Rle}{Lrm}{d.Day}{Lrm} ב{He["date.month_" + d.Month]} {Lrm}{d.Year}{Lrm}{Pdf}";
        var line = k.Line("objective-" + first.Id);
        var title = line.Substring("יעד ".Length, line.IndexOf(": הושלם", System.StringComparison.Ordinal) - "יעד ".Length);
        var expected = "יעד " + title + ": הושלם ב־" + Date(first.ReferenceDate.AddMonths(-1)) + "; בהיסטוריה ב־" + Date(first.ReferenceDate);
        Assert.IsTrue(string.Equals(expected, line, System.StringComparison.Ordinal), "expected\n" + Escape(expected) + "\nbut was\n" + Escape(line));
        var open = C.Objectives.First(o => o.Id != first.Id);   // Ended completes only the first
        var openLine = k.Line("objective-" + open.Id);
        var openTitle = openLine.Substring("יעד ".Length, openLine.IndexOf(": לא הושלם", System.StringComparison.Ordinal) - "יעד ".Length);
        var openExpected = "יעד " + openTitle + ": לא הושלם; בהיסטוריה ב־" + Date(open.ReferenceDate);
        Assert.IsTrue(string.Equals(openExpected, openLine, System.StringComparison.Ordinal), "expected\n" + Escape(openExpected) + "\nbut was\n" + Escape(openLine));
        var anchor = EndReport.For(C, k.Bridge.State).PopulationHistory!;
        Assert.GreaterOrEqual(k.Line("population").IndexOf(Date(anchor.Date), System.StringComparison.Ordinal), 0, "the population line's date too: " + Escape(k.Line("population")));
    }

    /// <summary>The line with its invisible marks spelled out, for a failure message.</summary>
    static string Escape(string s) => string.Concat(s.Select(c => c < 0x2000 || c > 0x206F ? c.ToString() : $"<U+{(int)c:X4}>"));
}
