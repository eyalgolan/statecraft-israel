#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The UI's only door into the simulation (spec section 6). Holds the GameSession, collects this turn's
    /// orders, runs End Turn and tells the screens to refresh. Orders take effect at End Turn, in the order given; each
    /// order replaces an earlier one with the same key (one choice per event, one call-up level, one budget, one appointee
    /// per office, one policy level per policy, one build per hex, one work order per city, one training order per city and
    /// track, one cancel per cohort, one pioneer order per unit).</summary>
    public sealed class SimBridge
    {
        readonly List<Command> queue = new List<Command>();

        public GameSession Session { get; private set; }
        public Content Content => Session.Content;
        public GameState State => Session.State;
        public IReadOnlyList<Command> Queued => queue;
        /// <summary>The orders the last turn ran with (empty until a turn has run, and after a refused batch): what the player
        /// chose, as against what the turn delegated (OutcomePresenter plays a clip only for a choice).</summary>
        public IReadOnlyList<Command> LastBatch { get; private set; } = Array.Empty<Command>();
        /// <summary>The game date when the last turn's orders were given (the date before that turn ran).</summary>
        public DateTime LastBatchDate { get; private set; }
        /// <summary>The step before Session.Last (null until two turns have run), for "change since last turn". The session
        /// keeps it, so a loaded save has it from its replay (#398).</summary>
        public StepResult? BeforeLast => Session.BeforeLast;
        /// <summary>Raised after every change the screens show: an order queued or withdrawn, a turn, a load.</summary>
        public event Action? Changed;
        /// <summary>Raised after End Turn with the orders the simulation refused (possibly none).</summary>
        public event Action<IReadOnlyList<Rejection>>? TurnEnded;
        /// <summary>Raised when Queue refuses an order, with the player-facing reason.</summary>
        public event Action<string>? Refused;

        readonly UiText text;
        readonly Func<GameSession, IReadOnlyList<Command>, StepResult> step;

        /// <param name="step">How a turn is run; the default is Session.Step. Tests pass a stepper that throws, to prove
        /// that a batch the simulation refuses outright cannot soft-lock the game.</param>
        public SimBridge(GameSession session, UiText text, Func<GameSession, IReadOnlyList<Command>, StepResult>? step = null)
        {
            this.step = step ?? ((s, cmds) => s.Step(cmds));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>The order's slot in the queue: a later order with the same key replaces the earlier one. Internal for
        /// SimBridgeGuardTests, which checks that every command type is listed here or is one per turn by design.</summary>
        internal static string Key(Command c) => c switch
        {
            ChooseEventOption e => "event:" + e.EventId,
            SetPolicy p => "policy:" + p.Policy,
            Appoint a => "appoint:" + a.Office,
            MapAction m => FormattableString.Invariant($"map:{m.Q},{m.R}"),
            AssignWorkers w => "work:" + w.City,
            Train t => "train:" + t.City + ":" + t.Track,
            CancelTraining x => FormattableString.Invariant($"untrain:{x.Cohort}"),
            ReturnCraftsmen r => "retcraft:" + r.City,
            ReassignCraftsmen r => "regcraft:" + r.City,
            ReassignPioneers r => "regpioneer:" + r.City + ":" + r.Track,
            Reinforce x => FormattableString.Invariant($"reinforce:{x.Unit}"),
            PioneerWork w => FormattableString.Invariant($"unit:{w.Unit}"),
            StopWork w => FormattableString.Invariant($"unit:{w.Unit}"),
            UnitOrder o => FormattableString.Invariant($"order:{o.Unit}"),
            StartProject p => FormattableString.Invariant($"project:{p.Q},{p.R}"),
            CancelProject x => FormattableString.Invariant($"unproject:{x.Project}"),
            Move m => FormattableString.Invariant($"move:{m.Unit}"),
            ResolveCapture r => "capture:" + r.Settlement,
            EndOccupation e => "endocc:" + e.Settlement,
            _ => c.Kind,
        };

        /// <summary>The order's Attention price at this turn's prices: what <see cref="Queue"/> charges and the card shows.</summary>
        public double Price(Command c) => AttentionSystem.Price(Content, State, c);

        /// <summary>Whether the Attention left pays for the order (a queued order with its key is refunded first): the one
        /// check <see cref="Queue"/> refuses on, so the event card (#1034) and a refusal can never disagree.</summary>
        public bool CanPay(Command c) => Price(c) <= LeftFor(c) + 1e-9;

        double LeftFor(Command c) { var old = QueuedLike(c); return AttentionLeft + (old != null ? Price(old) : 0); }

        /// <summary>Who End Turn hands the event to with the orders queued now (Simulation.DelegatedTo), or null for the
        /// cabinet: an appointment queued this turn is in office by then (#1034).</summary>
        public Person? DelegatedTo(GameEvent e) => Simulation.DelegatedTo(Content, State, queue, e.Id);

        /// <summary>The vacant seats the player can fill, less those an appointment queued now fills (Cabinet.Vacancies): the
        /// side menu's Cabinet badge (#1109).</summary>
        public IReadOnlyList<Office> Vacancies => Cabinet.Vacancies(Content, State, queue);

        /// <summary>Whether the Budget screen's <paramref name="draft"/> is not what End turn will apply and Submit can apply it
        /// (EconomySystem.Unsubmitted): the side menu's Budget badge (#1109).</summary>
        public bool BudgetUnsubmitted(IReadOnlyDictionary<string, double> draft) => EconomySystem.Unsubmitted(Content, State, draft, queue);

        /// <summary>Attention left after paying for the queued orders at this turn's prices.</summary>
        public double AttentionLeft => AttentionSystem.Left(Content, State, queue);

        /// <summary>The queued order with this key, or null.</summary>
        public Command? QueuedLike(Command c) => queue.FirstOrDefault(q => Key(q) == Key(c));

        /// <summary>Null if the id is known, else the player-facing reason. The player sees only the kind of thing (#622: a
        /// content id means nothing to them), in the UI's language: the string order.kind.&lt;kind&gt; (#595). The id goes to
        /// the log, for a bug report.</summary>
        string? Unknown(string kind, string? id, IEnumerable<string> known)
        {
            if (!string.IsNullOrEmpty(id) && known.Contains(id)) return null;
            UnityEngine.Debug.Log($"Order refused: unknown {kind.Replace('_', ' ')} '{id}'");
            return text.Format("order.unknown", text["order.kind." + kind]);
        }

        /// <summary>Checks every id an order carries against the content, so End Turn can never be handed an id the
        /// simulation would throw on. Returns the player-facing reason, or null.</summary>
        string? Validate(Command c)
        {
            switch (c)
            {
                case SetPolicy p: return Unknown("policy", p.Policy, Content.Policies.Select(x => x.Id));
                case Appoint a: return Unknown("office", a.Office, Content.Offices.Select(x => x.Id)) ?? Unknown("person", a.Person, Content.People.Select(x => x.Id));
                case ChooseEventOption e:
                    var ev = Content.Events.FirstOrDefault(x => x.Id == e.EventId);
                    return ev == null ? Unknown("event", e.EventId, new string[0]) : Unknown("option", e.OptionId, ev.Options.Select(x => x.Id));
                case MapAction m: return Unknown("action", m.Action, MapActions.Actions);
                case AllocateBudget b:
                    if (b.Shares == null) return text["order.invalid"];
                    foreach (var k in b.Shares.Keys) { var r = Unknown("budget_line", k, Content.Rules.BudgetStart.Keys); if (r != null) return r; }
                    return null;
                case SetMobilization _: case SecurityOrder _: return null;
                // The GDD orders (#380): ids only. Roles, counts and limits are the sim's, which refuses them with its own
                // reason and refunds the Attention. The legacy content has no cities, so it queues none of these.
                case AssignWorkers w: return Unknown("city", w.City, Content.Cities.Select(x => x.Id));
                case Train t:
                    return Unknown("city", t.City, Content.Cities.Select(x => x.Id))
                        ?? Unknown("training", t.Track, Content.UnitTypes.Where(x => !string.IsNullOrEmpty(x.Facility)).Select(x => x.Id));
                // Return and reassignment (#1511): the city id only; the sim refuses a count, a track or a busy civilian with its own reason.
                case ReturnCraftsmen r: return Unknown("city", r.City, Content.Cities.Select(x => x.Id));
                case ReassignCraftsmen r: return Unknown("city", r.City, Content.Cities.Select(x => x.Id));
                case ReassignPioneers r: return Unknown("city", r.City, Content.Cities.Select(x => x.Id));
                case CancelTraining x:
                    return Unknown("cohort", x.Cohort.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Cohorts.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                // Replacements (#1510): ids only; the sim refuses a full, enemy or misplaced unit and the training checks itself.
                case Reinforce x:
                    return Unknown("unit", x.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                        ?? Unknown("city", x.City, Content.Cities.Select(k => k.Id));
                // Pioneer work (#378, camp/field/swamp #1124, corridors #786): ids only; the sim refuses a busy unit, a built site or segment, a kind
                // mismatch or a short Treasury with its own reason. A corridor action names "corridor:N", N from 1.
                case PioneerWork w:
                    return Unknown("unit", w.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                        ?? Unknown("pioneer_work", w.Action, new[] { Projects.BuildYishuv, Projects.BuildCamp, Projects.PlowFields, Projects.DrySwamp, Corridors.BuildRoad, Corridors.BuildRail })
                        ?? (Corridors.IsAction(w.Action)
                            ? Unknown("corridor_segment", w.Site, Content.Corridors.SelectMany(x => Enumerable.Range(1, x.Segments.Count).Select(n => x.Id + ":" + n.ToString(System.Globalization.CultureInfo.InvariantCulture))))
                            : Unknown("site", w.Site, Content.Sites.Select(x => x.Id)));
                case StopWork w: return Unknown("unit", w.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                // Movement (#744): the unit id only; the sim refuses an unreachable, full or forbidden hex with its own reason.
                case Move m: return Unknown("unit", m.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                // Standing orders (#746): the unit id only; the sim refuses an unknown order, a halutz or an enemy unit itself.
                case UnitOrder o: return Unknown("unit", o.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                // City projects (#735): ids only; the sim refuses a far or taken hex, the policy gate and the turn's limit itself.
                case StartProject p:
                    return Unknown("city_project", p.ProjectKind, Content.ProjectKinds.Select(x => x.Id)) ?? Unknown("city", p.City, Content.Cities.Select(x => x.Id));
                case CancelProject x:
                    return Unknown("city_project", x.Project.ToString(System.Globalization.CultureInfo.InvariantCulture), State.CityProjects.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                // Buying Arms (#781): no ids; the sim refuses a count below one itself.
                case BuyArms _: return null;
                // Answering a capture (#797): the sim refuses a place not awaiting the choice, or an outcome not built yet, with
                // its own reason and never throws on the id. The capture dialog is PR 10.
                case ResolveCapture _: return null;
                // Answering the end of an occupation (#799): the sim refuses a place not awaiting the end with its own reason.
                case EndOccupation _: return null;
                default: return text["order.invalid"];
            }
        }

        string? Refuse(string reason) { Refused?.Invoke(reason); return reason; }

        static void Notify(Action? handlers)
        {
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
                try { handler(); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        static void Notify<T>(Action<T>? handlers, T value)
        {
            if (handlers == null) return;
            foreach (Action<T> handler in handlers.GetInvocationList())
                try { handler(value); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        /// <summary>Queues an order (replacing one with the same key). Returns null, or why it was refused here: an id
        /// the content does not have, the game is over, or the order costs more attention than is left. A refusal is
        /// also raised as <see cref="Refused"/>.</summary>
        public string? Queue(Command c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var bad = Validate(c);
            if (bad != null) return Refuse(bad);
            if (State.GameOver != null) return Refuse(text["order.game_over"]);
            if (c is AllocateBudget ab) c = new AllocateBudget(new Dictionary<string, double>(ab.Shares));   // a snapshot: later edits to the caller's dictionary must not change the order
            if (!CanPay(c))
                return Refuse(text.Format("order.no_attention", Price(c), LeftFor(c)));
            var old = QueuedLike(c);
            if (old != null) queue[queue.IndexOf(old)] = c; else queue.Add(c);
            Changed?.Invoke();
            return null;
        }

        public void Withdraw(Command c)
        {
            if (queue.Remove(c)) Changed?.Invoke();
        }

        /// <summary>Runs the turn with the queued orders, clears the queue, and notifies. If the simulation refuses the
        /// batch outright (any exception: an ArgumentException is a refusal, anything else is also logged), the state is unchanged, the queue is cleared, and the reason is raised
        /// through TurnEnded like any refusal.</summary>
        public StepResult EndTurn()
        {
            StepResult r;
            var batch = queue.ToList();
            var date = State.Date;
            try { r = step(Session, batch); }
            catch (Exception e)   // any exception (M3 final review M7): never leave a queue that fails on every End Turn
            {
                // A refusal (ArgumentException) is a clear warning, anything else the full exception: both leave a trace.
                if (e is ArgumentException) UnityEngine.Debug.LogWarning("End Turn refused: " + e.Message);
                else UnityEngine.Debug.LogException(e);
                queue.Clear();
                LastBatch = Array.Empty<Command>();
                var reason = text["order.failed"];   // the message stays in the log above, not on screen (#622)
                var failed = new StepResult(State, new Cause[0], batch.Select(x => new Rejection(x, reason)).ToList());
                Notify(Changed);
                Notify(TurnEnded, failed.Rejected);
                return failed;
            }
            LastBatch = batch; LastBatchDate = date;
            queue.Clear();
            Notify(Changed);
            Notify(TurnEnded, r.Rejected);
            return r;
        }

        /// <summary>Switches to another session (a loaded save), dropping queued orders.</summary>
        public void Replace(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            LastBatch = Array.Empty<Command>();
            queue.Clear();
            Changed?.Invoke();
        }
    }
}
