using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The Appoint command: who may be appointed, what it costs, and how long it lasts.</summary>
public class CabinetTests
{
    const string ExtraPeople = @"
  - id: outsider
    name: Out Sider
    living: false
    party: beta
    competence: 9
    loyalty: 3
    offices: []
  - id: loyalist
    name: Loy Alist
    living: false
    party: alpha
    competence: 2
    loyalty: 9
    offices: []
  - id: loyal_two
    name: Loyal Two
    living: false
    party: alpha
    competence: 6
    loyalty: 9
    offices: []
  - id: late
    name: Late Comer
    living: false
    died: 1948-07-01
    party: alpha
    competence: 5
    loyalty: 5
    offices: []
  - id: gone
    name: Gone Early
    living: false
    died: 1948-05-01
    competence: 5
    loyalty: 5
    offices: []
";

    static Content Load(Dictionary<string, string>? events = null) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = Fixture.Files["people.yaml"] + ExtraPeople }, events));

    static readonly Content C = Load();
    static readonly IReadOnlyList<Command> None = new Command[0];

    [Fact]
    public void AppointingReplacesTheMinisterAndCostsCoalitionStability()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        var why = new List<Cause>();
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "loyalist", why));
        Assert.Equal("loyalist", Cabinet.Holder(C, s, "finance_minister")!.Id);
        Assert.Equal("def_two", Offices.Holder(C, "finance_minister", s.Date)!.Id);   // history itself is unchanged
        Assert.Equal(0.9, s.CoalitionStability, 9);                                    // Def Two had no party: a seat lost
        Assert.Contains(why, w => w.System == "cabinet" && w.Message == "Loy Alist appointed Minister of Finance, replacing Def Two");
        Assert.Contains(why, w => w.Target == "coalition_stability" && w.Message == "Coalition stability -10.0% (Def Two lost the Minister of Finance)");
    }

    [Fact]
    public void AReshuffleWithinAPartyCostsLess()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        var why = new List<Cause>();
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "loyal_two", why));
        Assert.Equal(0.9 - 0.03, s.CoalitionStability, 9);
        Assert.Contains(why, w => w.Message == "Coalition stability -3.0% (Alpha resents the reshuffle)");
    }

    [Theory]
    [InlineData("prime_minister", "loyalist", "prime_minister", "You are the Prime Minister: that office is not yours to fill")]
    [InlineData("finance_minister", "outsider", "party_not_coalition", "Out Sider's party (Beta) is not in the coalition")]
    [InlineData("finance_minister", "gone", "died", "Gone Early died on 1 May 1948")]
    [InlineData("finance_minister", "def_two", "already_holds", "Def Two already holds the office of Minister of Finance")]
    [InlineData("finance_minister", "nobody", "person_unknown", "That person is not in this game")]
    [InlineData("treasurer", "loyalist", "office_unknown", "That office does not exist in this game")]
    public void IllegalAppointmentsAreRejectedWithAStableCodeAndUnchangedEnglish(string office, string person, string kind, string reason)
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        var before = s.Hash();
        var coded = Cabinet.CheckReason(C, s, office, person)!;
        Assert.Equal(kind, coded.Kind);
        Assert.Equal(reason, coded.Text);
        Assert.Equal(reason, Cabinet.Appoint(C, s, office, person, new List<Cause>()));
        Assert.Equal(before, s.Hash());
    }

    [Fact]
    public void ARejectedAppointCommandIsRefundedAndReported()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "outsider") });
        Assert.Equal("Out Sider's party (Beta) is not in the coalition", r.Rejected.Single().Reason);
        Assert.Equal("def_two", Cabinet.Holder(C, r.State, "finance_minister")!.Id);
    }

    // ---- refusal paths that need offices and people the plain fixture lacks ----
    const string RichOffices = @"
  - id: president
    title: President of Israel
  - id: chief_of_staff
    title: IDF Chief of the General Staff
  - id: old_office
    title: Old Office
    until: 1948-12-01
";
    const string RichPeople = @"
  - id: pres
    name: Pres Ident
    living: false
    competence: 5
    loyalty: 5
    offices:
      - office: president
        from: 1948-05-14
  - id: cos
    name: Cos One
    living: false
    competence: 6
    loyalty: 6
    offices:
      - office: chief_of_staff
        from: 1948-05-14
";
    static readonly Content Rich = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
    {
        ["offices.yaml"] = Fixture.Files["offices.yaml"] + RichOffices,
        ["people.yaml"] = Fixture.Files["people.yaml"] + ExtraPeople + RichPeople,
    }));

    [Fact]
    public void TheSittingPresidentCannotBeAppointed()
    {
        var s = Simulation.NewGame(Rich, 42, Mode.Historical);
        Assert.Equal("Pres Ident is the President", Cabinet.Check(Rich, s, "finance_minister", "pres"));
    }

    [Fact]
    public void TheServingChiefOfStaffCannotBeAppointedToAnotherOffice()
    {
        var s = Simulation.NewGame(Rich, 42, Mode.Historical);
        Assert.Equal("Cos One is the serving Chief of Staff", Cabinet.Check(Rich, s, "finance_minister", "cos"));
    }

    [Fact]
    public void NoGovernmentInOfficeIsARefusal()
    {
        var s = new GameState { Date = new DateTime(1948, 1, 1) };           // before every government
        Assert.Equal("No government is in office", Cabinet.Check(C, s, "finance_minister", "loyalist"));
    }

    [Fact]
    public void OnlyACareerOfficerCanBeChiefOfStaff()
    {
        var s = Simulation.NewGame(Rich, 42, Mode.Historical);
        Assert.Equal("Loy Alist is not an IDF career officer", Cabinet.Check(Rich, s, "chief_of_staff", "loyalist"));
    }

    [Fact]
    public void AnOfficeIsNotASeatOrAppointableBeforeItIsEstablished()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["offices.yaml"] = Fixture.Files["offices.yaml"] + "\n  - { id: new_office, title: New Office, from: 1949-01-01 }\n",
            ["people.yaml"] = Fixture.Files["people.yaml"] + ExtraPeople,
        }));
        var before = new GameState { Date = new DateTime(1948, 12, 31) };
        Assert.DoesNotContain(Cabinet.Lineup(c, before), seat => seat.Office.Id == "new_office");
        Assert.Equal("The office of New Office is not established until 1 January 1949", Cabinet.Check(c, before, "new_office", "loyalist"));
        // An appointment from an older save does not make anyone hold an office that does not exist yet.
        before.Appointments["new_office"] = new Appointment("loyalist", before.Date);
        before.AppointedUnder = Offices.Government(c, before.Date)!.Id;
        Assert.Null(Cabinet.Holder(c, before, "new_office"));
        var on = new GameState { Date = new DateTime(1949, 1, 1) };
        Assert.Contains(Cabinet.Lineup(c, on), seat => seat.Office.Id == "new_office" && seat.Holder == null);
        Assert.NotEqual("The office of New Office is not established until 1 January 1949", Cabinet.Check(c, on, "new_office", "loyalist"));
    }

    [Fact]
    public void AnAbolishedOfficeCannotBeFilled()
    {
        var s = new GameState { Date = new DateTime(1948, 12, 1) };
        Assert.Equal("The office of Old Office was abolished on 1 December 1948", Cabinet.Check(Rich, s, "old_office", "loyalist"));
    }

    /// <summary>Step refills attention at the end of the turn, so the spend is observed through what the same turn can still
    /// afford: at the starting rung one appointment costs 2 of the 3 points.</summary>
    [Fact]
    public void ARejectedAppointCostsNoAttention()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var cost = AttentionSystem.Cost(C, s0, Cabinet.Kind);
        Assert.True(cost > s0.Attention / 2, "the fixture must make two appointments unaffordable");
        var bad = new Appoint("finance_minister", "outsider");
        var r = Simulation.Step(C, s0, new Command[] { bad, bad, bad });
        Assert.All(r.Rejected, x => Assert.Equal("Out Sider's party (Beta) is not in the coalition", x.Reason));   // never "Not enough attention"
        Assert.Equal(3, r.Rejected.Count);
    }

    [Fact]
    public void ALegalAppointSpendsTheFixtureCost()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var cost = AttentionSystem.Cost(C, s0, Cabinet.Kind);
        var r = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "loyalist"), new Appoint("defense_minister", "loyal_two") });
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Not enough attention (need {0:0.0}, have {1:0.0})", cost, s0.Attention - cost),
            r.Rejected.Single().Reason);
    }

    [Fact]
    public void ThePartyRuleFollowsTheCoalitionOfTheDay()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1949, 3, 10);                                   // Government Two: Alpha and Beta
        Assert.Null(Cabinet.Check(C, s, "finance_minister", "outsider"));
    }

    [Fact]
    public void AppointmentsEndWhenANewGovernmentTakesOffice()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s = Simulation.Step(C, s, new Command[] { new Appoint("finance_minister", "loyalist") }).State;
        Assert.Equal("loyalist", Cabinet.Holder(C, s, "finance_minister")!.Id);
        StepResult r;
        do { r = Simulation.Step(C, s, None); s = r.State; } while (Offices.Government(C, s.Date)!.Id == "gov1");
        Assert.Equal("def_two", Cabinet.Holder(C, s, "finance_minister")!.Id);
        Assert.Empty(s.Appointments);
        Assert.Contains(r.Why, w => w.Message == "A new government took office with its own ministers; your appointments ended");
    }

    [Fact]
    public void AnAppointeeWhoDiesLeavesOfficeOnTheDateOfDeath()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "late", new List<Cause>()));
        Assert.Equal("late", Cabinet.Holder(C, s, "finance_minister", new DateTime(1948, 6, 30))!.Id);
        Assert.Equal("def_two", Cabinet.Holder(C, s, "finance_minister", new DateTime(1948, 7, 1))!.Id);
    }

    [Fact]
    public void TheAppointeesCompetenceDrivesTheMinistrysEffects()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Equal(0.7, EffectsSystem.Multiplier(C, s, "finance_minister"), 9);   // Def Two, competence 4
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        Assert.Equal(0.6, EffectsSystem.Multiplier(C, s, "finance_minister"), 9);   // Loy Alist, competence 2
    }

    [Fact]
    public void EventTextNamesTheAppointee()
    {
        var c = Load(new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event });
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Cabinet.Appoint(c, s, "finance_minister", "loyalist", new List<Cause>());
        var drought = c.Events.Single(e => e.Id == "drought");
        Assert.Equal("Loy Alist reports a drought.", EventSystem.Render(c, s, drought));
    }

    [Fact]
    public void HistoricalOnlyOfficesKeepTheirTermsButAreNotCabinetSeatsOrAppointable()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["offices.yaml"] = Fixture.Files["offices.yaml"] +
                "\n  - { id: historical_title, title: Historical title, historical_only: true }\n",
            ["people.yaml"] = Fixture.Files["people.yaml"].Replace("office: finance_minister", "office: historical_title")
        }));
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Assert.Equal("def_two", Offices.Holder(c, "historical_title", s.Date)?.Id);
        Assert.DoesNotContain(Cabinet.Lineup(c, s), seat => seat.Office.Id == "historical_title");
        Assert.Equal("Historical title is a historical title, not an appointable office",
            Cabinet.Check(c, s, "historical_title", "pm_one"));
        Assert.All(Cabinet.Candidates(c, s, "historical_title"), candidate => Assert.NotNull(candidate.reason));
        Assert.NotNull(Cabinet.Appoint(c, s, "historical_title", "pm_one", new List<Cause>()));
        Assert.Empty(s.Appointments);
        s.AppointedUnder = Offices.Government(c, s.Date)!.Id;
        s.Appointments["historical_title"] = new Appointment("pm_one", s.Date);
        Assert.Equal("def_two", Cabinet.Holder(c, s, "historical_title")?.Id);
        Assert.Equal("Def Two", Cabinet.Fill(c, s, "{historical_title}", s.Date));
    }

    [Fact]
    public void TheLineupListsEveryExistingOfficeAndMarksAppointees()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        var lineup = Cabinet.Lineup(C, s);
        Assert.Equal(new[] { "prime_minister", "defense_minister", "finance_minister" }, lineup.Select(x => x.Office.Id));
        Assert.True(lineup.Single(x => x.Office.Id == "finance_minister").Appointed);
        Assert.False(lineup.Single(x => x.Office.Id == "defense_minister").Appointed);
    }

    [Fact]
    public void AppointmentsAreInTheHashAndStepNeverMutatesItsInput()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var plain = Simulation.Step(C, s0, None).State;
        var appointed = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "loyalist") }).State;
        Assert.Empty(s0.Appointments);
        Assert.NotEqual(plain.Hash(), appointed.Hash());
    }

    [Fact]
    public void RealContentRules()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = new GameState { Date = new DateTime(1951, 1, 1) };
        Assert.Equal("Golda Meir is not an IDF career officer", Cabinet.Check(c, s, "chief_of_staff", "golda_meir"));
        Assert.Equal("The office of Minister of War Victims was abolished on 8 October 1951",
            Cabinet.Check(c, new GameState { Date = new DateTime(1951, 11, 1) }, "war_victims_minister", "maimon"));
        Assert.Equal("Aharon Zisling's party (Mapam) is not in the coalition", Cabinet.Check(c, s, "agriculture_minister", "zisling"));
        // The Provisional Government had no education portfolio: the ministry dates from 10 March 1949.
        var provisional = new GameState { Date = new DateTime(1948, 6, 12) };
        Assert.DoesNotContain(Cabinet.Lineup(c, provisional), seat => seat.Office.Id == "education_minister");
        Assert.Equal("The office of Minister of Education and Culture is not established until 10 March 1949",
            Cabinet.Check(c, provisional, "education_minister", "shazar"));
        Assert.Contains(Cabinet.Lineup(c, new GameState { Date = new DateTime(1949, 3, 10) }), seat => seat.Office.Id == "education_minister" && seat.Holder?.Id == "shazar");
        // #217: no office in the 1948 lineup is vacant; the presidency and the minister without portfolio begin later.
        Assert.All(Cabinet.Lineup(c, provisional), seat => Assert.NotNull(seat.Holder));
        Assert.Equal(new DateTime(1949, 3, 10), c.Offices.Single(o => o.Id == "rationing_minister").From);
        Assert.Equal(new DateTime(1949, 2, 16), c.Offices.Single(o => o.Id == "president").From);
        Assert.Equal(new DateTime(1951, 10, 8), c.Offices.Single(o => o.Id == "minister_without_portfolio").From);
        Assert.Equal("The President is elected by the Knesset, not appointed", Cabinet.Check(c, s, "president", "sharett"));
        Assert.Null(Cabinet.Check(c, s, "agriculture_minister", "eshkol"));
        Assert.Equal("Chaim Weizmann is the President", Cabinet.Check(c, s, "finance_minister", "weizmann"));
        Assert.Equal("Yigael Yadin is the serving Chief of Staff", Cabinet.Check(c, s, "finance_minister", "yadin"));
        s.Date = new DateTime(1951, 6, 1);
        Assert.Equal("David Remez died on 19 May 1951", Cabinet.Check(c, s, "transportation_minister", "remez"));
    }
}
