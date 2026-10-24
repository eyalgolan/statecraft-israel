using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The public entry point the Unity layer calls: start a game, then step it one turn at a time.</summary>
    public static class Simulation
    {
        public static GameState NewGame(Content c, ulong seed, Mode mode)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (mode == Mode.Open) throw new NotSupportedException("Open mode arrives with milestone M4.");
            var s = new GameState { Date = c.Rules.Start, RngState = seed, Mode = mode };
            var why = new List<Cause>();
            EffectsSystem.Init(c, s);
            EconomySystem.Init(c, s);
            PopulationSystem.Init(c, s);
            Cities.Init(c, s);
            Pools.Init(c, s);
            StartingForces.Init(c, s);   // R-UNIT-006: before any event can grant a unit, so the forces take ids 0 to n-1
            SecuritySystem.Init(c, s, why);
            AttentionSystem.Refill(c, s);
            EventSystem.Trigger(c, s, why);
            return s;
        }

        /// <summary>Applies commands in order (each pays attention first; a command that fails validation is refunded
        /// and carries a reason; a vote that is held and lost is charged and reported as a politics cause), delegates
        /// unanswered events, then advances one turn. Never mutates <paramref name="state"/>.</summary>
        public static StepResult Step(Content c, GameState state, IReadOnlyList<Command> commands)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (commands.Any(x => x == null)) throw new ArgumentException("Commands cannot contain null.", nameof(commands));
            CommandText.Validate(commands);

            var s = state.Clone();
            var why = new List<Cause>();
            var rejected = new List<Rejection>();
            if (s.GameOver != null)
            {
                foreach (var cmd in commands) rejected.Add(new Rejection(cmd, $"The game is over: {PlayerText.GameOver(s.GameOver)}"));
                return new StepResult(s, why, rejected);
            }

            RunOrders(c, s, commands, new Dictionary<int, int>(), new Dictionary<int, (int Q, int R)>(), why, rejected);
            EventSystem.Delegate(c, s, why);

            var len = SecuritySystem.TurnLengthFor(c, s.Date, s.Rung);
            // The next era's start, or the game's end (the last era's end, or mvp.end in the GDD loop, R-LOOP-004), is a
            // boundary: the turn that reaches it ends on it, with that turn's real days (spec 4.1, 4.5).
            var end = c.End;
            var boundary = c.Eras.Select(e => e.From).Where(f => f > s.Date).Concat(new[] { end }).Min();
            var next = SimTime.AdvanceTo(s.Date, len, boundary);
            if (c.IsGdd) SettlementControl.Stamp(s, next);   // R-CTRL-004: dates for captures made during the orders
            var adjusted = next != SimTime.Advance(s.Date, len);
            var days = (next - s.Date).Days;
            var yf = adjusted ? days / 365.0 : SimTime.YearFraction(len);
            TurnResolver.Resolve(c, s, next, yf, why);   // the GDD v0.1 loop only; the legacy loop goes straight on
            EffectsSystem.Apply(c, s, why);
            PopulationSystem.Apply(c, s, next, yf, why);
            EconomySystem.Apply(c, s, yf, why);
            PoliticsSystem.UpdateStability(c, s, yf, why);
            if (adjusted) SecuritySystem.Apply(c, s, days / 7, why);   // whole weeks of the adjusted turn
            else SecuritySystem.Apply(c, s, len, why);
            PoliticsSystem.ReportChanges(c, s.Date, next, why);
            var turnStart = s.Date;
            s.Date = next;
            s.Turn++;
            if (s.GameOver == null && s.Date >= end)
            {
                // #19's legacy report is EndReport.For, which the end screen reads once GameOver is set (D-H4)
                s.GameOver = end == c.Eras[c.Eras.Count - 1].To ? GameOverCodes.EraEnded : GameOverCodes.MvpEnded;
                why.Add(new Cause("era", s.GameOver, 0, PlayerText.GameOver(s.GameOver)));
            }
            ContentWindows.Update(c, s, turnStart);
            Cabinet.ExpireOnGovernmentChange(c, s, why);
            AttentionSystem.Refill(c, s);
            if (s.GameOver == null) EventSystem.Trigger(c, s, why, turnStart);   // a game that just ended raises no new events
            return new StepResult(s, why, rejected);
        }

        /// <summary>Who End Turn would hand the event to (EventSystem.Delegate) if the turn ended with these orders: runs
        /// them on a clone, as <see cref="Step"/> does, then asks EventSystem.DelegatedTo. Never mutates <paramref name="state"/>.</summary>
        public static Person? DelegatedTo(Content c, GameState state, IReadOnlyList<Command> commands, string eventId)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (commands.Any(x => x == null)) throw new ArgumentException("Commands cannot contain null.", nameof(commands));
            CommandText.Validate(commands);
            var s = state.Clone();
            if (s.GameOver == null) RunOrders(c, s, commands, new Dictionary<int, int>(), new Dictionary<int, (int Q, int R)>(), new List<Cause>(), new List<Rejection>());
            return EventSystem.DelegatedTo(c, s, c.Events.First(e => e.Id == eventId));
        }

        /// <summary>The state after <paramref name="commands"/> run as End turn would run them first (spec 2026-10-23 section 3;
        /// RunOrders, shared with Step), on a clone. Never mutates <paramref name="state"/>.</summary>
        public static PreviewResult Preview(Content c, GameState state, IReadOnlyList<Command> commands)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (commands.Any(x => x == null)) throw new ArgumentException("Commands cannot contain null.", nameof(commands));
            CommandText.Validate(commands);
            var s = state.Clone();
            var why = new List<Cause>(); var rejected = new List<Rejection>(); var spent = new Dictionary<int, int>();
            var attacked = new Dictionary<int, (int Q, int R)>();
            if (s.GameOver != null)
                foreach (var cmd in commands) rejected.Add(new Rejection(cmd, $"The game is over: {PlayerText.GameOver(s.GameOver)}"));
            else RunOrders(c, s, commands, spent, attacked, why, rejected);
            return new PreviewResult(s, spent, attacked, why, rejected);
        }

        /// <summary>A move, a go-to, an attack, fortify or hold takes effect at once; Send home and every other order waits
        /// for End turn (spec 2026-10-23 section 3.2).</summary>
        public static bool IsInstant(Command cmd) => cmd is Move || (cmd is UnitOrder o && o.Order != Demobilization.SendHome);

        /// <summary>The turn's orders in order: each pays Attention first; one that fails validation is refunded and
        /// rejected with its reason. Step, DelegatedTo and Preview share it, so every dry run sees what End Turn will.</summary>
        static void RunOrders(Content c, GameState s, IReadOnlyList<Command> commands, Dictionary<int, int> spent,
                              Dictionary<int, (int Q, int R)> attacked, List<Cause> why, List<Rejection> rejected)
        {
            if (c.IsGdd) Movement.ContinueRoutes(c, s, spent, why);   // R-MOVE-009: the route step, before the turn's orders
            if (c.IsGdd) SettlementControl.CaptureHeld(c, s, why);
            var priceRung = s.Rung;   // every command this turn is priced at the rung the turn started at
            foreach (var cmd in commands)
            {
                if (!AttentionSystem.IsCommandKind(cmd.Kind)) { rejected.Add(new Rejection(cmd, "That order is not known")); continue; }
                // A GDD order has no price in the legacy rules: refuse it before charging (R-CITY-001).
                if (AttentionSystem.IsGddKind(cmd.Kind) && !c.IsGdd) { rejected.Add(new Rejection(cmd, "That order is not available in this game")); continue; }
                // The call-up and the security order are retired in the GDD loop (R-WAR-002): refused the same way,
                // before charging, so the state and the Attention are unchanged.
                if (c.IsGdd && (cmd is SetMobilization || cmd is SecurityOrder)) { rejected.Add(new Rejection(cmd, "That order is not available in this game")); continue; }
                var before = s.Attention;
                var error = AttentionSystem.TrySpend(c, s, cmd.Kind, priceRung) ?? Execute(c, s, cmd, spent, attacked, why);
                if (error != null) { s.Attention = before; rejected.Add(new Rejection(cmd, error)); }
                if (c.IsGdd) SettlementControl.CaptureHeld(c, s, why);   // R-CTRL-004: only where an order ends
            }
        }

        /// <summary>Returns null when the command was carried out (a lost vote counts: it consumes the RNG draw and is
        /// reported as a politics cause), or the reason it failed validation. A command that fails validation leaves
        /// the state as it found it (attention is refunded by the caller).</summary>
        static string? Execute(Content c, GameState s, Command cmd, Dictionary<int, int> spent,
                               Dictionary<int, (int Q, int R)> attacked, List<Cause> why)
        {
            switch (cmd)
            {
                case SetPolicy p:
                    if (c.Policies.All(x => x.Id != p.Policy)) return "That policy does not exist in this game";
                    if (c.At(s.Date).Policies.All(x => x.Id != p.Policy)) return "That policy is not available at this time";
                    if (!(p.Level >= 0 && p.Level <= 1)) return "Policy level must be between 0 and 1";
                    var legislature = Offices.Legislature(c, s.Date);
                    if (legislature == null) return "No legislature is sitting";
                    if (!PassVote(c, s))
                    {
                        if (Advisory(c, s))
                            why.Add(new Cause("politics", p.Policy, 0, $"{legislature.Body} voted against {PlayerText.Target(c, p.Policy)} at {PlayerText.Percent(p.Level)}, but its vote does not bind"));
                        else
                        {
                            why.Add(new Cause("politics", p.Policy, 0, $"{legislature.Body} voted down {PlayerText.Target(c, p.Policy)} at {PlayerText.Percent(p.Level)}"));
                            return null;
                        }
                    }
                    s.Policies[p.Policy] = p.Level;
                    why.Add(new Cause("policy", p.Policy, p.Level, $"{PlayerText.Target(c, p.Policy)} set to {PlayerText.Percent(p.Level)}"));
                    return null;
                case AllocateBudget b:
                {
                    // Validate before voting, so a malformed allocation neither draws from the RNG nor blames the legislature.
                    var old = s.Budget;
                    var invalid = EconomySystem.Allocate(c, s, b.Shares);
                    if (invalid != null) return invalid;
                    if (PoliticsSystem.BudgetNeedsVote(c, s.Date) && !PassVote(c, s))
                    {
                        if (Advisory(c, s))
                            why.Add(new Cause("politics", "budget", 0, $"{Offices.Legislature(c, s.Date)!.Body} voted against the budget, but its vote does not bind"));
                        else
                        {
                            s.Budget = old;
                            why.Add(new Cause("politics", "budget", 0, $"{Offices.Legislature(c, s.Date)!.Body} voted the budget down"));
                        }
                    }
                    return null;
                }
                case ChooseEventOption e:
                    return EventSystem.Choose(c, s, e.EventId, e.OptionId, why);
                case SetMobilization m:
                    return SecuritySystem.SetMobilization(c, s, m.Level, why);
                case Appoint a:
                    return Cabinet.Appoint(c, s, a.Office, a.Person, why);
                case MapAction m:
                    return MapActions.Apply(c, s, m.Action, m.Q, m.R, why);
                case AssignWorkers w:
                    return Cities.Assign(c, s, w.City, w.Role, w.Count, why);
                case Train t:
                    return Training.Start(c, s, t.City, t.Track, t.Count, why);
                case CancelTraining t:
                    return Training.Cancel(c, s, t.Cohort, why);
                case ReturnCraftsmen r:
                    return Qualification.ReturnCraftsmen(c, s, r.City, r.Count, why);
                case ReassignCraftsmen r:
                    return Qualification.ReassignCraftsmen(c, s, r.City, r.Count, why);
                case ReassignPioneers r:
                    return Qualification.ReassignPioneers(c, s, r.City, r.Track, r.Count, why);
                case Reinforce r:
                    return Training.Reinforce(c, s, r.Unit, r.City, r.Count, why);
                case PioneerWork p:
                    return Projects.Start(c, s, p.Unit, p.Action, p.Site, why);
                case StopWork w:
                    return Projects.Stop(c, s, w.Unit, why);
                case UnitOrder o:
                    if (o.Order == Movement.GoTo) return Movement.GiveGoTo(c, s, o.Unit, o.Q, o.R, spent, why);
                    if (o.Order == Combat.Attack) return Combat.Strike(c, s, o.Unit, o.Q, o.R, spent, attacked, why);
                    if (o.Q != null || o.R != null) return "That order names no hex";
                    if (o.Order == Qualification.ReturnOrder) return Qualification.Return(c, s, o.Unit, why);
                    return o.Order == Demobilization.SendHome
                        ? Demobilization.Give(c, s, o.Unit, attacked, why)
                        : Orders.Give(c, s, o.Unit, o.Order, why);
                case StartProject p:
                    return Projects.StartCity(c, s, p.ProjectKind, p.City, p.Q, p.R, why);
                case CancelProject p:
                    return Projects.CancelCity(c, s, p.Project, why);
                case Move m:
                    return Movement.Move(c, s, m.Unit, m.Q, m.R, spent, why);
                case BuyArms a:
                    return Pools.BuyArms(c, s, a.Count, why);
                case ResolveCapture x:
                    return SettlementControl.Resolve(c, s, x.Settlement, x.Outcome, why);
                case SecurityOrder o:
                    // long arithmetic: an extreme delta clamps to the ladder's end instead of wrapping around.
                    SecuritySystem.SetRung(c, s, (int)Math.Max(1L, Math.Min(5L, (long)s.Rung + o.Delta)), why);
                    return null;
                default:
                    return "That order is not known";
            }
        }

        /// <summary>True in an era whose legislature is an assembly (eras.yaml systems.legislature): its vote is held but does not bind.</summary>
        static bool Advisory(Content c, GameState s) => c.SystemsAt(s.Date).Legislature == EraSystems.Assembly;

        static bool PassVote(Content c, GameState s)
        {
            var rng = new Rng(s.RngState);
            var passed = PoliticsSystem.Vote(c, s, ref rng);
            s.RngState = rng.State;
            return passed;
        }
    }
}
