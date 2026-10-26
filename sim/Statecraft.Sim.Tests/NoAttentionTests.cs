using Statecraft.Sim;
using Statecraft.Sim.Reference;
using System.Text.RegularExpressions;

public class NoAttentionTests
{
    public static IEnumerable<object[]> Kinds() => CommandGuardTests.Samples.Values.Select(c => new object[] { c.Kind });
    static void Select(Dictionary<string,string> files, bool modern)
    {
        if (modern) files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
        files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(),Gdd03BalanceCatalogue.FileName));
    }
    static Content Load(bool modern = true)
    {
        var files=Fixture.Gdd(); Select(files,modern); return ContentLoader.Load(Fixture.Write(files));
    }
    static void Poison(Content c)
    {
        foreach (var r in c.EraRules.Values.Append(c.Rules)) { r.AttentionCost.Clear(); r.AttentionPerTurn=double.NaN; r.RungAttentionStep=double.NaN; }
    }
    static string WithoutLegacyAccounting(string canonical) =>
        Regex.Replace(canonical.Replace("ruleset=gdd-0.3;", ""), @"\natt=[^\n]*", "");
    static bool Unavailable(string kind) => kind is "map_action" or "set_mobilization" or "security_order";
    static (Content c,GameState s,Command valid,Command invalid) Case(string kind,bool modern)
    {
        var c = kind is "resolve_capture" or "end_occupation"
            ? CaptureTests.Load(files=>Select(files,modern),CaptureTests.AllTargets) : Load(modern);
        var s=Simulation.NewGame(c,1,Mode.Historical); s.Attention=500; s.NextId=1000;
        Command valid=CommandGuardTests.Samples.Values.Single(x=>x.Kind==kind), invalid=valid;
        switch(kind)
        {
            case "set_policy": valid=new SetPolicy("rationing",.5); invalid=new SetPolicy("missing",.5); break;
            case "allocate_budget": valid=new AllocateBudget(new Dictionary<string,double>{{"finance_minister",1}}); invalid=new AllocateBudget(new Dictionary<string,double>{{"missing",1}});break;
            case "choose_event_option":
                c.Events.Add(new GameEvent { Id="test", Office="prime_minister", Options=new List<EventOption>{new EventOption {Id="yes",Text="Test"}} });
                s.PendingEvents.Add("test"); valid=new ChooseEventOption("test","yes"); invalid=new ChooseEventOption("test","missing");break;
            case "appoint": valid=new Appoint("finance_minister","pm_one"); invalid=new Appoint("missing","pm_one");break;
            case "assign_workers":valid=new AssignWorkers("town_a","job",1);invalid=new AssignWorkers("town_a","job",-1);break;
            case "train":valid=new Train("town_a","halutz",1);invalid=new Train("town_a","halutz",0);break;
            case "cancel_training": s.Cohorts[200]=new Cohort(200,"halutz","town_a",1,2);valid=new CancelTraining(200);invalid=new CancelTraining(999);break;
            case "return_craftsmen":s.Cities["town_a"]=s.Cities["town_a"].WithCraftsmen(1);valid=new ReturnCraftsmen("town_a",1);invalid=new ReturnCraftsmen("town_a",0);break;
            case "reassign_craftsmen":s.Cities["town_a"]=s.Cities["town_a"].WithQualified(new Qualified(1,0,Array.Empty<int>()));valid=new ReassignCraftsmen("town_a",1);invalid=new ReassignCraftsmen("town_a",0);break;
            case "reassign_pioneers":s.Cities["town_a"]=s.Cities["town_a"].WithQualified(new Qualified(0,0,new[]{0}));valid=new ReassignPioneers("town_a","halutz",1);invalid=new ReassignPioneers("town_a","halutz",0);break;
            case "reinforce":s.Units[99]=new Unit(99,"recruit",1,0,side:"israel",strength:1,full:2);s.Arms=100;valid=new Reinforce(99,"town_a",1);invalid=new Reinforce(999,"town_a",1);break;
            case "pioneer_work":s.Units[99]=new Unit(99,"halutz",1,0);valid=new PioneerWork(99,Projects.BuildYishuv,"site_a");invalid=new PioneerWork(999,Projects.BuildYishuv,"site_a");break;
            case "stop_work":s.Units[99]=new Unit(99,"halutz",1,0);s.Sites["site_a"]=new SiteWork("site_a",99,3,null);valid=new StopWork(99);invalid=new StopWork(999);break;
            case "start_project":valid=new StartProject("housing","town_a",1,1);invalid=new StartProject("missing","town_a",1,1);break;
            case "cancel_project":s.CityProjects[200]=new CityProject(200,"housing","town_a",1,1,2);valid=new CancelProject(200);invalid=new CancelProject(999);break;
            case "move":s.Units[99]=new Unit(99,"recruit",1,0,side:"israel",strength:1);valid=new Move(99,2,0);invalid=new Move(999,2,0);break;
            case "unit_order":s.Units[99]=new Unit(99,"recruit",1,0,side:"israel",strength:1);valid=new UnitOrder(99,"fortify");invalid=new UnitOrder(999,"fortify");break;
            case "buy_arms":valid=new BuyArms(1);invalid=new BuyArms(0);break;
            case "resolve_capture":s.Units[99]=CaptureTests.Soldier(99,1,0);s.Settlements["town_a"]=new SettlementControl("israel",SettlementControl.AwaitingChoice,s.Date);valid=new ResolveCapture("town_a","raid");invalid=new ResolveCapture("town_a","missing");break;
            case "end_occupation":s.Units[99]=CaptureTests.Soldier(99,1,0);s.Settlements["town_a"]=new SettlementControl("israel",SettlementControl.AwaitingEnd,s.Date);valid=new EndOccupation("town_a","withdraw");invalid=new EndOccupation("town_a","missing");break;
        }
        return(c,s,valid,invalid);
    }
    [Fact] public void CoversEveryConcreteCommand()
    {
        Assert.Equal(23,Kinds().Count());
        Assert.Equal(CommandGuardTests.Samples.Keys.OrderBy(t=>t.Name),typeof(Command).Assembly.GetTypes().Where(t=>t.IsSubclassOf(typeof(Command))&&!t.IsAbstract).OrderBy(t=>t.Name));
    }
    [Theory] [MemberData(nameof(Kinds))]
    public void SetupProvesSuccessfulOldModePathForEveryAvailableKind(string kind)
    {
        var(c,s,valid,invalid)=Case(kind,false);var p=Simulation.Preview(c,s,new[]{valid});
        if(Unavailable(kind)) Assert.Contains("not part of this game",p.Rejected.Single().Reason.Replace("not available in this game","not part of this game"));
        else {Assert.Empty(p.Rejected);Assert.NotEmpty(Simulation.Preview(c,s,new[]{invalid}).Rejected);}
    }
    // R-GOV-002: absence branches before touching legacy accounting, while real validation still runs.
    [Theory] [MemberData(nameof(Kinds))]
    public void EveryKindKeepsRealValidationWithoutReadingOrChangingOldPool(string kind)
    {
        var(c,s,valid,invalid)=Case(kind,true);Poison(c);s.Attention=123.456;
        var good=Simulation.Preview(c,s,new[]{valid});
        if(Unavailable(kind)) Assert.Single(good.Rejected); else Assert.Empty(good.Rejected);
        var bad=Simulation.Preview(c,s,new[]{invalid});Assert.Single(bad.Rejected);
        Assert.DoesNotContain("attention",bad.Rejected[0].Reason,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(123.456,s.Attention);Assert.Equal(123.456,good.State.Attention);Assert.Equal(123.456,bad.State.Attention);
        var step=Simulation.Step(c,s,new[]{valid});Assert.Equal(123.456,step.State.Attention);
        var(oldContent,oldState,oldValid,oldInvalid)=Case(kind,false);
        var oldGood=Simulation.Preview(oldContent,oldState,new[]{oldValid});
        var oldBad=Simulation.Preview(oldContent,oldState,new[]{oldInvalid});
        Assert.Equal(oldBad.Rejected.Single().Reason,bad.Rejected.Single().Reason);
        Assert.Equal(WithoutLegacyAccounting(oldGood.State.Describe()),WithoutLegacyAccounting(good.State.Describe()));
        Assert.Equal(WithoutLegacyAccounting(oldBad.State.Describe()),WithoutLegacyAccounting(bad.State.Describe()));
        Assert.Equal(WithoutLegacyAccounting(Simulation.Step(oldContent,oldState,new[]{oldValid}).State.Describe()),WithoutLegacyAccounting(step.State.Describe()));
    }
    [Fact] public void InitialAndEndTurnRefillAreAbsentAndCanonicalStateIgnoresCompatibilityPool()
    {
        var c=Load();Poison(c);var s=Simulation.NewGame(c,1,Mode.Historical);
        Assert.Equal(c.Ruleset,s.Ruleset);Assert.Equal(c.Ruleset,s.Clone().Ruleset);
        Assert.False(typeof(GameState).GetProperty(nameof(GameState.Ruleset))!.CanWrite);
        Assert.DoesNotContain("att=",s.Describe());Assert.Contains("ruleset=gdd-0.3",s.Describe());
        var hash=s.Hash();s.Attention=double.NaN;Assert.Equal(hash,s.Hash());Assert.Equal(hash,s.Clone().Hash());
        Assert.True(double.IsNaN(Simulation.Step(c,s,Array.Empty<Command>()).State.Attention));
    }
    [Fact] public void LegacyResourceApisRefuseNewModeEvenForEmptyQueuesAndUnknownKinds()
    {
        var c=Load();var s=Simulation.NewGame(c,1,Mode.Historical);Poison(c);
        foreach(var call in new Action[]{()=>AttentionSystem.Refill(c,s),()=>AttentionSystem.Cost(c,s,"missing"),()=>AttentionSystem.Price(c,s,new BuyArms(1)),()=>AttentionSystem.Left(c,s,Array.Empty<Command>()),()=>AttentionSystem.TrySpend(c,s,"buy_arms")})
            Assert.Throws<InvalidOperationException>(call);
    }
    [Fact] public void QueriesCaptureOptionsAndHistoricalAiExpressAbsence()
    {
        var(c,s,_,_)=Case("resolve_capture",true);Poison(c);s.Attention=double.NaN;
        var captures=SettlementControl.CaptureOptions(c,s,"town_a").Where(o=>o.Preview!=null).ToList();Assert.NotEmpty(captures);
        Assert.All(captures,o=>Assert.Null(o.Preview!.Attention));
        var orders=HistoricalAI.Commands(c,s);Assert.Contains(orders,x=>x is ResolveCapture);
        Cabinet.Vacancies(c,s,Array.Empty<Command>());
        Assert.True(EconomySystem.Unsubmitted(c,s,new Dictionary<string,double>{{"defense_minister",1}},Array.Empty<Command>()));
        s.Settlements["town_a"]=new SettlementControl("israel",SettlementControl.AwaitingEnd,s.Date);
        var endings=SettlementControl.EndOptions(c,s,"town_a").Where(o=>o.Preview!=null).ToList();Assert.NotEmpty(endings);
        Assert.All(endings,o=>Assert.Null(o.Preview!.Attention));
        Assert.True(double.IsNaN(s.Attention));
    }
    sealed class UnknownOrder : Command { public override string Kind => "unknown_order"; }
    [Fact] public void UnknownCommandsAndGameOverRemainGuardedWithoutAccounting()
    {
        var c=Load();Poison(c);var s=Simulation.NewGame(c,1,Mode.Historical);s.Attention=12;
        Assert.Throws<ArgumentException>(()=>Simulation.Preview(c,s,new Command[]{new UnknownOrder()}));
        s.GameOver=GameOverCodes.MvpEnded;
        Assert.Contains("game is over",Simulation.Step(c,s,new Command[]{new BuyArms(1)}).Rejected.Single().Reason);
        Assert.Equal(12,Simulation.Preview(c,s,Array.Empty<Command>()).State.Attention);
    }
    [Fact] public void EveryReferenceGoldenEntryPointRefusesBeforeRunningEvenZeroTurns()
    {
        var c=Load();var s=Simulation.NewGame(c,1,Mode.Historical);Poison(c);
        foreach(var call in new Action[]{()=>ReferencePlayer.Commands(c,s),()=>GoldenRun.Orders(c,s),()=>GoldenRun.SmokeOrders(c,s),()=>GoldenRun.SmokeMove(c,s),()=>GoldenRun.Play(c,s.Date),()=>GoldenRun.PlayToEnd(c),()=>GoldenRun.PlayEra(c,c.Eras[0].Id,s.Date.AddDays(1)),()=>GoldenRun.PlaySmoke(c,0),()=>GoldenRun.PlaySmokeOrders(c)})
            Assert.Throws<NotSupportedException>(call);
    }
    [Fact] public void SessionCommitEndTurnAndReplayIgnoreOldPool()
    {
        var c=Load();Poison(c);var g=GameSession.New(c,1,Mode.Historical);g.State.Attention=55;
        g.State.Units[99]=new Unit(99,"recruit",1,0,side:"israel",strength:1);g.State.NextId=100;
        Assert.Null(g.Commit(new Move(99,2,0)));Assert.Equal(55,g.Now.State.Attention);
        g.EndTurn(Array.Empty<Command>());Assert.Equal(55,g.State.Attention);
        var text=g.Save.Write();Assert.Equal(4,SaveGame.Parse(text,c.Ruleset).Version);
        // Use a fresh session without injected units for actual replay of an authored deferred command.
        var clean=GameSession.New(c,1,Mode.Historical);clean.Step(new[]{new AssignWorkers("town_a","job",1)});
        Assert.Equal(clean.State.Hash(),GameSession.Load(c,clean.Save.Write(),out _).State.Hash());
    }
    [Fact] public void OpenTurnSchema4ReplaysWithoutInjectedStateAndRejectsTampering()
    {
        var files=Fixture.Gdd(new Dictionary<string,string> {
            ["map/units_1948.yaml"]="units:\n- id: test_force\n  service: army\n  side: israel\n  status: draft\n  strength: { value: 1000, as_of: '1948-05-15' }\n  anchors:\n  - { place: Here, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n",
            ["map/unit_anchor_hexes.yaml"]="anchors:\n  - { id: test_force, anchor: 0, q: 1, r: 0 }\n"
        });
        Select(files,true);var c=ContentLoader.Load(Fixture.Write(files));Poison(c);
        var g=GameSession.New(c,1,Mode.Historical);var unit=Assert.Single(g.State.Units.Values).Id;
        g.State.Attention=456;
        Assert.Null(g.Commit(new Move(unit,2,0)));var text=g.Save.Write();
        var replay=GameSession.Load(c,text,out var warnings);Assert.Empty(warnings);
        Assert.Equal(g.Now.State.Hash(),replay.Now.State.Hash());Assert.Single(replay.Save.Open);
        var changed=text.Replace($"move {unit} 2 0",$"move {unit} 99 99");Assert.NotEqual(text,changed);
        Assert.Throws<SaveException>(()=>GameSession.Load(c,changed,out _));
        replay.EndTurn(Array.Empty<Command>());Assert.Equal(2,replay.State.Units[unit].Q);
    }
    public static IEnumerable<object[]> OldLoaderCases()
    {
        foreach(string identity in new[]{SaveGame.LegacyRuleset,SaveGame.GddRuleset,SaveGame.Gdd03Ruleset}) foreach(bool over in new[]{false,true})
        foreach(string fragment in new[]{"attention_cost: {}", "attention_per_turn: -1", "rung_attention_step: .nan", "attention_cost: { set_policy: -1 }", "attention_cost: { bogus: 1 }"})
            yield return new object[]{identity,over,fragment};
    }
    [Theory] [MemberData(nameof(OldLoaderCases))]
    public void LoaderPreservesStrictOldValidationAndIgnoresOnlyNewModeAccounting(string identity,bool over,string fragment)
    {
        bool modern=identity==SaveGame.Gdd03Ruleset, gdd=identity!=SaveGame.LegacyRuleset;
        var files=gdd?Fixture.Gdd():new Dictionary<string,string>();
        if(modern)Select(files,true);
        var original=gdd?files["rules.yaml"]:Fixture.Files["rules.yaml"];
        string field=fragment.Split(':')[0];
        var dir=Fixture.Write(files);
        if(over)
        {
            Directory.CreateDirectory(Path.Combine(dir,"rules"));File.WriteAllText(Path.Combine(dir,"rules","founding.yaml"),fragment+"\n");
        }
        else File.WriteAllText(Path.Combine(dir,"rules.yaml"),Regex.Replace(original,@"(?m)^"+field+@":.*$",fragment));
        // Empty override maps merge with the base; this has never removed an old cost.
        if(!modern && !(over && fragment=="attention_cost: {}")) Assert.Throws<ContentException>(()=>ContentLoader.Load(dir));
        else Assert.Equal(identity,ContentLoader.Load(dir).Ruleset);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void LoaderRequiresNoAttentionFieldsForNewModeIncludingEraOverrides(bool eraOverride)
    {
        var files=Fixture.Gdd();Select(files,true);
        files["rules.yaml"]=Regex.Replace(files["rules.yaml"],@"(?m)^(attention_per_turn|rung_attention_step|attention_cost):.*\n","");
        var dir=Fixture.Write(files);
        if(eraOverride){Directory.CreateDirectory(Path.Combine(dir,"rules"));File.WriteAllText(Path.Combine(dir,"rules","founding.yaml"),"attention_per_turn: .nan\nrung_attention_step: -1\nattention_cost: { not_a_command: -99 }\n");}
        Assert.False(ContentLoader.Load(dir).UsesAttention);
    }
}
