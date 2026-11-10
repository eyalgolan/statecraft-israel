using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Cli;
using Statecraft.Sim;

/// <summary>#3022: the CLI's Briefing prints a card's title and text as the game shows them (EventSystem.Title and
/// EventSystem.Render with the state: a road variant's, else the early or late text, names from the player's
/// appointments), not the card's ordinary title and text.</summary>
public class BriefingCliTests
{
    static string EventLine(Content c, GameState s, string id)
    {
        var output = new StringWriter();
        Briefing.Write(c, s, output);
        return output.ToString().Replace("\r\n", "\n").Split('\n').Single(x => x.StartsWith($"  EVENT {id}: ", StringComparison.Ordinal));
    }

    static string OptionLine(Content c, GameState s, string optionId)
    {
        var output = new StringWriter();
        Briefing.Write(c, s, output);
        return output.ToString().Replace("\r\n", "\n").Split('\n').Single(x => x.StartsWith($"    {optionId}: ", StringComparison.Ordinal));
    }

    // #3031: an option line names an office's holder as the game does (Cabinet.Fill with the state, EventCardPresenter),
    // so the player's appointee and not the historical holder. CabinetTests' fixture: Loy Alist replaces Def Two.
    // Red without it: the option line named Def Two.
    [Fact]
    public void AnOptionLineNamesThePlayersAppointee()
    {
        var c = CabinetTests.Load(new Dictionary<string, string>
        {
            ["drought.yaml"] = ValidationTests.Event.Replace("text: Wait for rain", "text: Ask {finance_minister} to wait for rain")
        });
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);
        Assert.Null(Cabinet.Appoint(c, s, "finance_minister", "loyalist", new List<Cause>()));
        s.PendingEvents.Add("drought");
        Assert.Equal("    wait: Ask Loy Alist to wait for rain", OptionLine(c, s, "wait"));
    }

    // R-ENEMY-007: a card shown after its window prints its late text. Real content, rhodes_talks on its `to`.
    // Red without it: the Briefing printed the ordinary text.
    [Fact]
    public void ACardShownLatePrintsItsLateText()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var e = c.Events.Single(x => x.Id == "rhodes_talks");
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        s.Date = e.To!.Value;
        s.PendingEvents.Add(e.Id);
        Assert.True(EventSystem.ShowsLate(e, s.Date));
        var line = EventLine(c, s, e.Id);
        Assert.Equal($"  EVENT {e.Id}: {EventSystem.Title(c, s, e)} - {EventSystem.Render(c, s, e)}", line);
        Assert.Contains(Cabinet.Fill(c, s, e.LateText, EventSystem.FillDate(e, s.Date)), line);
        Assert.DoesNotContain(Cabinet.Fill(c, s, e.Text, EventSystem.FillDate(e, s.Date)), line);
    }

    // R-WAR-008: a card shown before its `from` prints its early text. Gdd03ArmisticeOfferTests' fixture early text.
    // Red without it: the Briefing printed the ordinary text.
    [Fact]
    public void ACardShownEarlyPrintsItsEarlyText()
    {
        var c = Gdd03ArmisticeOfferTests.LoadEarly();
        var e = c.Events.Single(x => x.Id == "armistice_egypt");
        var s = CombatTests.With(c);
        s.Date = new DateTime(1948, 12, 15);
        s.PendingEvents.Add(e.Id);
        Assert.True(EventSystem.ShowsEarly(e, s.Date));
        var line = EventLine(c, s, e.Id);
        Assert.Equal($"  EVENT {e.Id}: {EventSystem.Title(c, s, e)} - {EventSystem.Render(c, s, e)}", line);
        Assert.Contains(Cabinet.Fill(c, s, Gdd03ArmisticeOfferTests.Early, s.Date), line);
        Assert.DoesNotContain("Israel decides whether to sign.", line);
    }
}
