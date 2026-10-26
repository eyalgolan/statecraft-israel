using Statecraft.Sim;

// R-POP-001/002: the fixture ledger must not feed the old scale, Food refill or population mutators.
public class PopulationLedgerBoundaryTests
{
    [Theory]
    [InlineData("population.init")] [InlineData("population.apply")]
    [InlineData("pools.init")] [InlineData("pools.consume")] [InlineData("pools.harvest")]
    [InlineData("pools.buy")] [InlineData("pools.term")]
    [InlineData("cities.init")] [InlineData("cities.assign")] [InlineData("cities.produce")]
    [InlineData("cities.house")] [InlineData("cities.condition")] [InlineData("cities.arrive")] [InlineData("cities.upkeep")]
    [InlineData("starting.init")] [InlineData("starting.arrive")]
    [InlineData("training.start")] [InlineData("training.cancel")] [InlineData("training.progress")] [InlineData("training.reinforce")]
    [InlineData("military.serve")] [InlineData("military.upkeep")]
    [InlineData("qualification.return")] [InlineData("qualification.craftsmen")] [InlineData("qualification.pioneers")] [InlineData("qualification.unit")]
    [InlineData("demobilization.give")]
    [InlineData("combat.resolve")] [InlineData("combat.outcomes")]
    [InlineData("prisoners.upkeep")] [InlineData("prisoners.return")] [InlineData("prisoners.take")]
    [InlineData("events.trigger")] [InlineData("events.choose")] [InlineData("events.delegate")] [InlineData("turn.resolve")]
    [InlineData("economy.flows")] [InlineData("economy.apply")] [InlineData("objectives.complete")]
    public void DirectAggregateCallsRefuseBeforeAnyStateResourceOrCauseMutation(string call)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] {
            new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) });
        s.Population["fixture"] = 2000; s.Treasury = 23; s.Debt = 2; s.Food = 7; s.FoodNeed = 123; s.Arms = 5; s.RngState = 99;
        var why = new List<Cause> { new Cause("fixture", "keep", 1, "keep") };
        var before = s.Describe(); var hash = s.Hash(); var cause = why.Single(); var rules = new MvpRules();
        void Invoke()
        {
            switch (call)
            {
                case "population.init": PopulationSystem.Init(c,s); break;
                case "population.apply": PopulationSystem.Apply(c,s,s.Date.AddMonths(1),.1,why); break;
                case "pools.init": Pools.Init(c,s); break;
                case "pools.consume": Pools.Consume(c,s,rules,why); break;
                case "pools.harvest": Pools.Harvest(c,s,rules,why); break;
                case "pools.buy": Pools.BuyArms(c,s,1,why); break;
                case "pools.term": Pools.FoodTerm(c,s); break;
                case "cities.init": Cities.Init(c,s); break;
                case "cities.assign": Cities.Assign(c,s,"missing","work",1,why); break;
                case "cities.produce": Cities.Produce(c,s,rules,.1,why); break;
                case "cities.house": Cities.House(c,s,rules,why); break;
                case "cities.condition": Cities.UpdateCondition(c,s,rules,why); break;
                case "cities.arrive": Cities.Arrive(c,s,rules,s.Date.AddMonths(1),why); break;
                case "cities.upkeep": Cities.AnnualUpkeep(c,s,rules); break;
                case "starting.init": StartingForces.Init(c,s); break;
                case "starting.arrive": StartingForces.Arrive(c,s,s.Date.AddMonths(1)); break;
                case "training.start": Training.Start(c,s,"missing","missing",1,why); break;
                case "training.cancel": Training.Cancel(c,s,0,why); break;
                case "training.progress": Training.Progress(c,s,why); break;
                case "training.reinforce": Training.Reinforce(c,s,0,"missing",1,why); break;
                case "military.serve": Military.Serve(s,rules,why); break;
                case "military.upkeep": Military.PayUpkeep(c,s,rules,why); break;
                case "qualification.return": Qualification.ReturnCraftsmen(c,s,"missing",1,why); break;
                case "qualification.craftsmen": Qualification.ReassignCraftsmen(c,s,"missing",1,why); break;
                case "qualification.pioneers": Qualification.ReassignPioneers(c,s,"missing","missing",1,why); break;
                case "qualification.unit": Qualification.Return(c,s,0,why); break;
                case "demobilization.give": Demobilization.Give(c,s,0,null,why); break;
                case "combat.resolve": Combat.Resolve(c,s,why); break;
                case "combat.outcomes": Combat.Outcomes(c,s,0); break;
                case "prisoners.upkeep": Prisoners.Upkeep(c,s,rules,why); break;
                case "prisoners.return": Prisoners.Return(c,s,"missing",why); break;
                case "prisoners.take": Prisoners.Take(s,new Unit(1,"missing",0,0,side:"missing",strength:1),new Unit(2,"missing",0,0,side:Movement.PlayerSide)); break;
                case "events.trigger": EventSystem.Trigger(c,s,why); break;
                case "events.choose": EventSystem.Choose(c,s,"missing","missing",why); break;
                case "events.delegate": EventSystem.Delegate(c,s,why); break;
                case "turn.resolve": TurnResolver.Resolve(c,s,s.Date.AddMonths(1),.1,why); break;
                case "economy.flows": EconomySystem.Flows(c,s,.1); break;
                case "economy.apply": EconomySystem.Apply(c,s,.1,why); break;
                case "objectives.complete": Objectives.Complete(c,s,rules,s.Date.AddMonths(1),why); break;
                default: throw new Exception("Unknown boundary fixture");
            }
        }
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(Invoke).Message);
        Assert.Equal(before,s.Describe()); Assert.Equal(hash,s.Hash()); Assert.Same(cause,Assert.Single(why));
    }

    // R-POP-003: reject lossy UTF-16 before accepting any persistent reference, but retain valid supplementary text.
    [Theory] [InlineData(0xD800, false)] [InlineData(0xD801, false)] [InlineData(0xDC00, false)] [InlineData(0xD800, true)]
    public void MalformedReferencesAreRejectedAtomically(int codeUnit, bool framed)
    {
        var bad = new string((char)codeUnit, 1);
        if (framed) bad = "x" + bad + "z";
        var p = PopulationLedgerTests.Person();
        var s = PopulationLedger.CreateFixture(PopulationLedgerTests.Content(),PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture",0,p) });
        var before = s.Describe();
        void Refuse(Action a) { Assert.ThrowsAny<ArgumentException>(a); Assert.Equal(before,s.Describe()); }
        Refuse(()=>new PopulationLocation(bad)); Refuse(()=>new PopulationLocation("north",facilityOrPort:bad));
        Refuse(()=>new PopulationTarget(bad,PopulationAssignmentKind.Work,"north"));
        Refuse(()=>new PopulationTarget("work",PopulationAssignmentKind.Work,bad));
        Refuse(()=>new PopulationAssignment(PopulationAssignmentKind.Work,bad));
        Refuse(()=>new PopulationEquipment(toolsId:bad)); Refuse(()=>new PopulationEquipment(militaryId:bad));
        Refuse(()=>new PopulationUnit(0,p.Status,1,p.Location,p.Assignment,p.Profession,p.Tier,7,bad,9,p.Equipment));
        Refuse(()=>new PopulationUnit(0,p.Status,1,p.Location,p.Assignment,p.Profession,p.Tier,7,"basic",9,p.Equipment,bad,p.Location));
        Refuse(()=>new PopulationBindings(new[]{p.Location!},Array.Empty<PopulationTarget>(),new[]{bad},Array.Empty<string>()));
        Refuse(()=>new PopulationBindings(new[]{p.Location!},Array.Empty<PopulationTarget>(),Array.Empty<string>(),new[]{bad}));
        Refuse(()=>new PopulationInventoryRow(bad,0,p));
        var newcomer = new PopulationUnit(0,p.Status,1,p.Location,p.Assignment,p.Profession,p.Tier,7,"basic",9,PopulationEquipment.None);
        Refuse(()=>s.PopulationLedger.RegisterArrival(bad,0,newcomer));
        Refuse(()=>s.PopulationLedger.RegisterCombatLoss(0,bad));
    }
    [Fact]
    public void SupplementaryUnicodeReferencesRemainLosslessAndDistinct()
    {
        var p = PopulationLedgerTests.Person();
        GameState S(string source) => PopulationLedger.CreateFixture(PopulationLedgerTests.Content(),PopulationLedgerTests.Bindings(),
            new[]{new PopulationInventoryRow(source,0,p)});
        var a=S("ship\U0001F6A2"); var b=S("ship\U0001F6A3");
        Assert.NotEqual(a.Hash(),b.Hash()); Assert.Equal(a.Hash(),a.Clone().Hash());
        a.PopulationLedger.RegisterCombatLoss(0,"loss\U0001F6A2"); a.PopulationLedger.Reconcile();
    }
    // R-POP-001/002: reception.turns is6, not the3 basic-credit catalogue value.
    [Fact]
    public void RealAgreedReceptionRequirementAcceptsFourAndSixAndRefusesSeven()
    {
        var real=ContentLoader.Load(RealContentTests.ContentRoot()); var c=PopulationLedgerTests.Content();
        c.Gdd03Balance=real.Gdd03Balance;
        Assert.Equal(6,c.RequirePopulationInteger("reception.turns"));
        var s=PopulationLedger.CreateFixture(c,PopulationLedgerTests.Bindings(),new[]{new PopulationInventoryRow("fixture",0,PopulationLedgerTests.Person())});
        foreach(var ticks in new[]{4,6}) { s.PopulationLedger.Replace(s.PopulationLedger.Get(0).WithReception(ticks)); s.PopulationLedger.Reconcile(); }
        var before=s.Describe(); Assert.Throws<ArgumentException>(()=>s.PopulationLedger.Replace(s.PopulationLedger.Get(0).WithReception(7)));
        Assert.Equal(before,s.Describe());
    }
}
