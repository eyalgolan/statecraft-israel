using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>A player (or AI) order for one turn. Kind keys rules.yaml attention_cost (see AttentionSystem.CommandKinds).</summary>
    public abstract class Command { public abstract string Kind { get; } }

    public sealed class SetPolicy : Command
    {
        public override string Kind => "set_policy";
        public string Policy { get; } public double Level { get; }
        public SetPolicy(string policy, double level) { Policy = policy; Level = level; }
    }

    /// <summary>Shares must cover exactly the offices in rules.yaml budget_start and sum to 1.</summary>
    public sealed class AllocateBudget : Command
    {
        public override string Kind => "allocate_budget";
        public Dictionary<string, double> Shares { get; }
        public AllocateBudget(Dictionary<string, double> shares) { Shares = shares; }
    }

    public sealed class ChooseEventOption : Command
    {
        public override string Kind => "choose_event_option";
        public string EventId { get; } public string OptionId { get; }
        public ChooseEventOption(string eventId, string optionId) { EventId = eventId; OptionId = optionId; }
    }

    public sealed class SetMobilization : Command
    {
        public override string Kind => "set_mobilization";
        public double Level { get; }
        public SetMobilization(double level) { Level = level; }
    }

    public sealed class SecurityOrder : Command
    {
        public override string Kind => "security_order";
        public int Delta { get; }
        public SecurityOrder(int delta) { Delta = delta; }
    }

    /// <summary>Puts <see cref="Person"/> in <see cref="Office"/> (Cabinet.Appoint has the rules).</summary>
    public sealed class Appoint : Command
    {
        public override string Kind => Cabinet.Kind;
        public string Office { get; } public string Person { get; }
        public Appoint(string office, string person) { Office = office; Person = person; }
    }

    /// <summary>Builds <see cref="Action"/> (MapActions.TransitCamp or MapActions.Housing) on hex (Q, R).</summary>
    public sealed class MapAction : Command
    {
        public override string Kind => MapActions.Kind;
        public string Action { get; } public int Q { get; } public int R { get; }
        public MapAction(string action, int q, int r) { Action = action; Q = q; R = r; }
    }

    /// <summary>Sets how many of <see cref="City"/>'s civilians work in <see cref="Role"/> (`job`), a standing order
    /// (Cities.Assign, R-CITY-001). GDD v0.1 loop only.</summary>
    public sealed class AssignWorkers : Command
    {
        public override string Kind => Cities.Kind;
        public string City { get; } public string Role { get; } public int Count { get; }
        public AssignWorkers(string city, string role, int count) { City = city; Role = role; Count = count; }
    }

    /// <summary>Starts <see cref="Count"/> of <see cref="City"/>'s civilians without work training as <see cref="Track"/> (a
    /// unit type with a facility: recruit or halutz; Training.Start, R-TRAIN-001). GDD v0.1 loop only.</summary>
    public sealed class Train : Command
    {
        public override string Kind => Training.TrainKind;
        public string City { get; } public string Track { get; } public int Count { get; }
        public Train(string city, string track, int count) { City = city; Track = track; Count = count; }
    }

    /// <summary>Ends training cohort <see cref="Cohort"/> early: its people return as civilians and the Money spent is not
    /// returned (Training.Cancel, R-TRAIN-002, D-A3). GDD v0.1 loop only.</summary>
    public sealed class CancelTraining : Command
    {
        public override string Kind => Training.CancelKind;
        public int Cohort { get; }
        public CancelTraining(int cohort) { Cohort = cohort; }
    }

    /// <summary>Returns <see cref="Count"/> of <see cref="City"/>'s craftsmen to civilian life, keeping their qualification
    /// (Qualification.ReturnCraftsmen, R-QUAL-002). GDD v0.1 loop only.</summary>
    public sealed class ReturnCraftsmen : Command
    {
        public override string Kind => Qualification.ReturnCraftsmenKind;
        public string City { get; } public int Count { get; }
        public ReturnCraftsmen(string city, int count) { City = city; Count = count; }
    }

    /// <summary>Puts <see cref="Count"/> of <see cref="City"/>'s qualified craftsmen back among its craftsmen, with no
    /// training (Qualification.ReassignCraftsmen, R-QUAL-004). GDD v0.1 loop only.</summary>
    public sealed class ReassignCraftsmen : Command
    {
        public override string Kind => Qualification.ReassignCraftsmenKind;
        public string City { get; } public int Count { get; }
        public ReassignCraftsmen(string city, int count) { City = city; Count = count; }
    }

    /// <summary>Puts <see cref="Count"/> of <see cref="City"/>'s qualified halutzim (<see cref="Track"/> `halutz`) or veteran
    /// pioneers (`veteran_halutz`) back on the map as units, with no training (Qualification.ReassignPioneers, R-QUAL-005).
    /// GDD v0.1 loop only.</summary>
    public sealed class ReassignPioneers : Command
    {
        public override string Kind => Qualification.ReassignPioneersKind;
        public string City { get; } public string Track { get; } public int Count { get; }
        public ReassignPioneers(string city, string track, int count) { City = city; Track = track; Count = count; }
    }

    /// <summary>Starts <see cref="Count"/> groups of <see cref="City"/>'s civilians without work training as replacements for
    /// damaged formation <see cref="Unit"/>, which stands on the city's settlement hex (Training.Reinforce, R-TRAIN-004; #1510).
    /// GDD v0.1 loop only.</summary>
    public sealed class Reinforce : Command
    {
        public override string Kind => Training.ReinforceKind;
        public int Unit { get; } public string City { get; } public int Count { get; }
        public Reinforce(int unit, string city, int count) { Unit = unit; City = city; Count = count; }
    }

    /// <summary>Sends halutz unit <see cref="Unit"/> to do pioneer work <see cref="Action"/> (build_yishuv) at designated site
    /// <see cref="Site"/> (Projects.Start, R-SITE-001). GDD v0.1 loop only.</summary>
    public sealed class PioneerWork : Command
    {
        public override string Kind => Projects.PioneerKind;
        public int Unit { get; } public string Action { get; } public string Site { get; }
        public PioneerWork(int unit, string action, string site) { Unit = unit; Action = action; Site = site; }
    }

    /// <summary>Stops unit <see cref="Unit"/>'s pioneer work: the site is available again, and the progress and the Money spent
    /// are lost (Projects.Stop, R-SITE-002, D-A3). GDD v0.1 loop only.</summary>
    public sealed class StopWork : Command
    {
        public override string Kind => Projects.StopKind;
        public int Unit { get; }
        public StopWork(int unit) { Unit = unit; }
    }

    /// <summary>Gives unit <see cref="Unit"/> the standing order <see cref="Order"/>: fortify or hold (Orders.Give, R-ORDER-001
    /// to R-ORDER-003), or attack, which names the hex (<see cref="Q"/>, <see cref="R"/>) (Combat.Give, R-COMBAT-001). GDD v0.1
    /// loop only.</summary>
    public sealed class UnitOrder : Command
    {
        public override string Kind => Orders.Kind;
        public int Unit { get; } public string Order { get; }
        /// <summary>The hex an attack names; null for fortify and hold.</summary>
        public int? Q { get; } public int? R { get; }
        public UnitOrder(int unit, string order, int? q = null, int? r = null) { Unit = unit; Order = order; Q = q; R = r; }
    }

    /// <summary>Starts city project <see cref="ProjectKind"/> (a projects.yaml id) for <see cref="City"/> on hex (<see cref="Q"/>,
    /// <see cref="R"/>) (Projects.StartCity, R-PROJ-001). GDD v0.1 loop only.</summary>
    public sealed class StartProject : Command
    {
        public override string Kind => Projects.StartProjectKind;
        public string ProjectKind { get; } public string City { get; } public int Q { get; } public int R { get; }
        public StartProject(string projectKind, string city, int q, int r) { ProjectKind = projectKind; City = city; Q = q; R = r; }
    }

    /// <summary>Cancels city project <see cref="Project"/> while it is being built: the Money spent is not returned
    /// (Projects.CancelCity, R-PROJ-002, D-A3). GDD v0.1 loop only.</summary>
    public sealed class CancelProject : Command
    {
        public override string Kind => Projects.CancelProjectKind;
        public int Project { get; }
        public CancelProject(int project) { Project = project; }
    }

    /// <summary>Moves unit <see cref="Unit"/> to hex (<see cref="Q"/>, <see cref="R"/>) along the cheapest path, within the
    /// movement points it has left this turn (Movement.Move, R-MOVE-001 to R-MOVE-006). GDD v0.1 loop only.</summary>
    public sealed class Move : Command
    {
        public override string Kind => Movement.Kind;
        public int Unit { get; } public int Q { get; } public int R { get; }
        public Move(int unit, int q, int r) { Unit = unit; Q = q; R = r; }
    }

    /// <summary>Buys <see cref="Count"/> Arms with Money (Pools.BuyArms, R-ARMS-002, D-G6). GDD v0.1 loop only.</summary>
    public sealed class BuyArms : Command
    {
        public override string Kind => Pools.BuyArmsKind;
        public int Count { get; }
        public BuyArms(int count) { Count = count; }
    }

    /// <summary>Answers the capture of <see cref="Settlement"/> with <see cref="Outcome"/>: raid, occupy or deport
    /// (SettlementControl.Resolve, R-CTRL-007 to R-CTRL-010). One kind for every outcome, so they cost the same Attention
    /// (Q-TECH-3). GDD v0.1 loop only.</summary>
    public sealed class ResolveCapture : Command
    {
        public override string Kind => SettlementControl.ResolveKind;
        public string Settlement { get; } public string Outcome { get; }
        public ResolveCapture(string settlement, string outcome) { Settlement = settlement; Outcome = outcome; }
    }

    /// <summary>Answers the end of <see cref="Settlement"/>'s occupation with <see cref="Outcome"/>: annex or withdraw
    /// (SettlementControl.End, R-CTRL-014 to R-CTRL-017; no puppet regime, Q-PUP-1 (c)). GDD v0.1 loop only.</summary>
    public sealed class EndOccupation : Command
    {
        public override string Kind => SettlementControl.EndKind;
        public string Settlement { get; } public string Outcome { get; }
        public EndOccupation(string settlement, string outcome) { Settlement = settlement; Outcome = outcome; }
    }

    public sealed class Rejection
    {
        public Command Command { get; } public string Reason { get; }
        public Rejection(Command command, string reason) { Command = command; Reason = reason; }
    }

    public sealed class StepResult
    {
        public GameState State { get; }
        public IReadOnlyList<Cause> Why { get; }
        public IReadOnlyList<Rejection> Rejected { get; }
        public StepResult(GameState state, IReadOnlyList<Cause> why, IReadOnlyList<Rejection> rejected) { State = state; Why = why; Rejected = rejected; }
    }

    /// <summary>The state after the turn's committed orders (spec 2026-10-23 section 3), before End turn: what
    /// the screen shows, the movement points each unit has spent, the turn's Why lines so far and the refused orders.</summary>
    public sealed class PreviewResult
    {
        public GameState State { get; }
        public IReadOnlyDictionary<int, int> Spent { get; }
        public IReadOnlyDictionary<int, (int Q, int R)> Attacked { get; }
        public IReadOnlyList<Cause> Why { get; }
        public IReadOnlyList<Rejection> Rejected { get; }
        public PreviewResult(GameState state, IReadOnlyDictionary<int, int> spent, IReadOnlyDictionary<int, (int Q, int R)> attacked,
                             IReadOnlyList<Cause> why, IReadOnlyList<Rejection> rejected)
        { State = state; Spent = spent; Attacked = attacked; Why = why; Rejected = rejected; }
    }

    /// <summary>Checks that every text an order carries can be saved: a null id or a lone surrogate would make the save
    /// unwritable or lossy, so Step refuses the whole batch before anything happens (a programmer error, like a null Command).</summary>
    static class CommandText
    {
        public static void Validate(IReadOnlyList<Command> commands)
        {
            foreach (var cmd in commands)
                switch (cmd)
                {
                    case SetPolicy p: Text(p.Policy, "Policy"); break;
                    case AllocateBudget b: if (b.Shares != null) foreach (var k in b.Shares.Keys) Text(k, "Shares key"); break;
                    case ChooseEventOption e: Text(e.EventId, "EventId"); Text(e.OptionId, "OptionId"); break;
                    case Appoint a: Text(a.Office, "Office"); Text(a.Person, "Person"); break;
                    case MapAction m: Text(m.Action, "Action"); break;
                    case AssignWorkers w: Text(w.City, "City"); Text(w.Role, "Role"); break;
                    case Train t: Text(t.City, "City"); Text(t.Track, "Track"); break;
                    case CancelTraining _: break;
                    case ReturnCraftsmen r: Text(r.City, "City"); break;
                    case ReassignCraftsmen r: Text(r.City, "City"); break;
                    case ReassignPioneers r: Text(r.City, "City"); Text(r.Track, "Track"); break;
                    case Reinforce r: Text(r.City, "City"); break;
                    case PioneerWork p: Text(p.Action, "Action"); Text(p.Site, "Site"); break;
                    case StopWork _: break;
                    case UnitOrder o: Text(o.Order, "Order"); break;
                    case StartProject p: Text(p.ProjectKind, "ProjectKind"); Text(p.City, "City"); break;
                    case CancelProject _: break;
                    case Move _: break;
                    case BuyArms _: break;
                    case ResolveCapture r: Text(r.Settlement, "Settlement"); Text(r.Outcome, "Outcome"); break;
                    case EndOccupation e: Text(e.Settlement, "Settlement"); Text(e.Outcome, "Outcome"); break;
                    case SetMobilization _: case SecurityOrder _: break;
                    default: throw new ArgumentException("unknown command type " + cmd.GetType().Name, "commands");
                }
        }

        static void Text(string? s, string field)
        {
            if (s == null) throw new ArgumentNullException("commands", $"{field} cannot be null.");
            if (!SaveText.IsEncodable(s)) throw new ArgumentException($"{field} is not valid text (a lone surrogate).", "commands");
        }
    }
}
