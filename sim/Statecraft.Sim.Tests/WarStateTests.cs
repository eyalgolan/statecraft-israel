using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Statecraft.Sim.Tests
{
    /// <summary>#750's first slice (D-C4; Q-WAR-1, Q-WAR-2, Q-WAR-4): R-WAR-002 retires the call-up and the security order
    /// in the GDD loop, where the call-up follows the rung instead, and R-WAR-003's clock half retires the 52-week clock
    /// there; an event's set_rung still moves the rung. The legacy loop keeps all of them while the code exists (D-E1; SecurityTests pins the legacy clock).</summary>
    public class WarStateTests
    {
        static Content Gdd(Dictionary<string, string>? extra = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));

        // R-WAR-002: the refusal reads as the not-available convention (R-CITY-001's wording), before charging, so the
        // state and the Attention are exactly what a turn without the order leaves.
        [Fact]
        public void TheCallUpIsRefusedInTheGddLoop()
        {
            var c = Gdd();
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            var refused = Simulation.Step(c, s, new Command[] { new SetMobilization(0.5) });
            var rejection = Assert.Single(refused.Rejected);
            Assert.Equal("That order is not available in this game", rejection.Reason);
            var clean = Simulation.Step(c, s, new Command[0]);
            Assert.Equal(3, refused.State.Attention);   // the fixture's attention_per_turn, untouched
            Assert.Equal(clean.State.Hash(), refused.State.Hash());
        }

        // R-WAR-002: the security order the ladder's buttons send.
        [Fact]
        public void TheSecurityOrderIsRefusedInTheGddLoop()
        {
            var c = Gdd();
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            var refused = Simulation.Step(c, s, new Command[] { new SecurityOrder(1) });
            Assert.Equal("That order is not available in this game", Assert.Single(refused.Rejected).Reason);
            Assert.Equal(5, refused.State.Rung);
            Assert.Equal(Simulation.Step(c, s, new Command[0]).State.Hash(), refused.State.Hash());
        }

        // R-WAR-002: HistoricalAI stops generating the retired call-up; below the rung's floor it would send one in the
        // legacy loop (EconomyM3Tests.HistoricalAiSendsReservesHomeToTheRungMinimum pins that side).
        [Fact]
        public void HistoricalAiSendsNoCallUpInTheGddLoop()
        {
            var c = Gdd();
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            s.Rung = 1;
            s.Mobilization = 0.5;   // above rung 1's 0.02 floor: the legacy loop would send SetMobilization(0.02)
            Assert.DoesNotContain(HistoricalAI.Commands(c, s), cmd => cmd is SetMobilization);
        }

        // R-WAR-003 (the slice's clock half): the 52-week countdown never ticks in the GDD loop, so it never ends the
        // war on its own.
        [Fact]
        public void TheCountdownNeverEndsTheWarInTheGddLoop()
        {
            var c = Gdd();
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            Assert.Equal(5, s.Rung);
            s.CeasefireWeeksLeft = 1;   // a monthly turn is more weeks than that
            var r = Simulation.Step(c, s, new Command[0]);
            Assert.Equal(5, r.State.Rung);
            Assert.Equal(1, r.State.CeasefireWeeksLeft);
            Assert.DoesNotContain(r.Why, w => w.Target == "ceasefire");
        }

        // R-WAR-003 (the slice's clock half): the clock never starts in the GDD loop, so no screen can promise a ceasefire
        // the sim will not impose; the legacy loop starts its 52 weeks on entering full war.
        [Fact]
        public void TheGddLoopStartsNoCeasefireClock()
        {
            var c = Gdd();
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            Assert.Equal(5, s.Rung);
            Assert.Equal(0, s.CeasefireWeeksLeft);
            Assert.Equal(0, SecuritySystem.CeasefireWeeksAfter(c, s.Date, 4, 0, 5));
            var legacy = ContentLoader.Load(Fixture.Write());
            Assert.Equal(legacy.Rules.CeasefireWeeks, Simulation.NewGame(legacy, 42, Mode.Historical).CeasefireWeeksLeft);
            Assert.Equal(legacy.Rules.CeasefireWeeks, SecuritySystem.CeasefireWeeksAfter(legacy, s.Date, 4, 0, 5));
        }

        const string RungEvent = @"
id: rung
title: A choice
text: '{finance_minister} asks for a decision.'
office: finance_minister
anchor: hard
from: 1948-05-15
options:
  - id: history
    text: The way it went
    historical: true
    set_rung: 3
";

        // Q-WAR-4 in the slice: an event's set_rung still moves the rung in the GDD loop.
        [Fact]
        public void AnEventSetRungStillMovesTheRungInTheGddLoop()
        {
            var c = Gdd(new Dictionary<string, string> { ["events/rung.yaml"] = RungEvent });
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            var r = Simulation.Step(c, s, new Command[] { new ChooseEventOption("rung", "history") });
            Assert.Empty(r.Rejected);
            Assert.Equal(3, r.State.Rung);
        }

        // R-WAR-002 (the owner's condition on #1148): with the call-up order retired, the reserves follow the rung down. A
        // rung an event sets lowers the call-up to that rung's rung_min_mobilization, as HistoricalAI's order did in the
        // legacy loop, so the 1949 armistices still send the army home (EconomyCalibrationTests keeps the debt anchor).
        [Fact]
        public void TheReservesGoHomeWithTheRungInTheGddLoop()
        {
            var c = Gdd(new Dictionary<string, string> { ["events/rung.yaml"] = RungEvent });
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            Assert.Equal(0.8, s.Mobilization);   // the rung-5 floor at the start
            var r = Simulation.Step(c, s, new Command[] { new ChooseEventOption("rung", "history") });
            Assert.Equal(3, r.State.Rung);
            Assert.Equal(c.Rules.RungMinMobilization[2], r.State.Mobilization);
            Assert.Contains(r.Why, w => w.Target == "mobilization" && w.Message == "Reserves sent home: call-up lowered to 15%, the minimum for security level 3");
        }

        // The legacy loop keeps the call-up as a lever: a lower rung leaves it where the player put it.
        [Fact]
        public void TheLegacyLoopLeavesTheCallUpWhenTheRungFalls()
        {
            var c = ContentLoader.Load(Fixture.Write());
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            var r = Simulation.Step(c, s, new Command[] { new SecurityOrder(-2) });
            Assert.Empty(r.Rejected);
            Assert.Equal(3, r.State.Rung);
            Assert.Equal(0.8, r.State.Mobilization);
        }

        // D-E1: the legacy loop keeps both orders and the clock while the code exists.
        [Fact]
        public void TheLegacyLoopKeepsTheCallUpAndTheClock()
        {
            var c = ContentLoader.Load(Fixture.Write());
            var s = Simulation.NewGame(c, 42, Mode.Historical);
            var r = Simulation.Step(c, s, new Command[] { new SetMobilization(0.5) });   // 2.0 attention at rung 5
            Assert.Empty(r.Rejected);
            Assert.Equal(0.8, r.State.Mobilization);   // the rung-5 floor raises the order, as the real game's 80%
            r = Simulation.Step(c, r.State, new Command[] { new SecurityOrder(-1) });
            Assert.Empty(r.Rejected);
            Assert.Equal(4, r.State.Rung);
            var war = Simulation.NewGame(c, 42, Mode.Historical);
            war.CeasefireWeeksLeft = 1;
            var cease = Simulation.Step(c, war, new Command[0]);   // the legacy era's turn is longer than a week
            Assert.NotEqual(5, cease.State.Rung);
            Assert.Contains(cease.Why, w => w.Target == "ceasefire");
        }
    }
}
