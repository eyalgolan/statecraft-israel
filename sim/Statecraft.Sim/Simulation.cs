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

            var priceRung = s.Rung;   // every command this turn is priced at the rung the turn started at
            foreach (var cmd in commands)
            {
                if (!AttentionSystem.IsCommandKind(cmd.Kind)) { rejected.Add(new Rejection(cmd, "That order is not known")); continue; }
                var before = s.Attention;
                var error = AttentionSystem.TrySpend(c, s, cmd.Kind, priceRung) ?? Execute(c, s, cmd, why);
                if (error != null) { s.Attention = before; rejected.Add(new Rejection(cmd, error)); }
            }
            EventSystem.Delegate(c, s, why);

            var len = SecuritySystem.TurnLengthFor(s.Rung);
            // The next era's start, or the last era's end, is a boundary: the turn that reaches it ends on it, with that turn's real days (spec 4.1, 4.5).
            var end = c.Eras[c.Eras.Count - 1].To;
            var boundary = c.Eras.Select(e => e.From).Where(f => f > s.Date).DefaultIfEmpty(end).Min();
            var next = SimTime.AdvanceTo(s.Date, len, boundary);
            var adjusted = next != SimTime.Advance(s.Date, len);
            var days = (next - s.Date).Days;
            var yf = adjusted ? days / 365.0 : SimTime.YearFraction(len);
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
                s.GameOver = GameOverCodes.EraEnded;   // the legacy report (#19) will come before this once it exists
                why.Add(new Cause("era", "era_ended", 0, PlayerText.GameOver(s.GameOver)));
            }
            Cabinet.ExpireOnGovernmentChange(c, s, why);
            AttentionSystem.Refill(c, s);
            if (s.GameOver == null) EventSystem.Trigger(c, s, why, turnStart);   // a game that just ended raises no new events
            return new StepResult(s, why, rejected);
        }

        /// <summary>Returns null when the command was carried out (a lost vote counts: it consumes the RNG draw and is
        /// reported as a politics cause), or the reason it failed validation. A command that fails validation leaves
        /// the state as it found it (attention is refunded by the caller).</summary>
        static string? Execute(Content c, GameState s, Command cmd, List<Cause> why)
        {
            switch (cmd)
            {
                case SetPolicy p:
                    if (c.Policies.All(x => x.Id != p.Policy)) return "That policy does not exist in this game";
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
