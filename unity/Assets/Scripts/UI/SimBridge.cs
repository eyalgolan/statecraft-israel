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
            PioneerWork w => FormattableString.Invariant($"unit:{w.Unit}"),
            StopWork w => FormattableString.Invariant($"unit:{w.Unit}"),
            _ => c.Kind,
        };

        double Price(Command c) => AttentionSystem.IsCommandKind(c.Kind) ? AttentionSystem.Cost(Content, State, c.Kind) : 0;

        /// <summary>Attention left after paying for the queued orders at this turn's prices.</summary>
        public double AttentionLeft => State.Attention - queue.Sum(Price);

        /// <summary>The queued order with this key, or null.</summary>
        public Command? QueuedLike(Command c) => queue.FirstOrDefault(q => Key(q) == Key(c));

        /// <summary>Null if the id is known, else the player-facing reason. The player sees only the kind of thing (#622: a
        /// content id means nothing to them); the id goes to the log, for a bug report.</summary>
        string? Unknown(string kind, string? id, IEnumerable<string> known)
        {
            if (!string.IsNullOrEmpty(id) && known.Contains(id)) return null;
            UnityEngine.Debug.Log($"Order refused: unknown {kind} '{id}'");
            return text.Format("order.unknown", kind);
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
                    foreach (var k in b.Shares.Keys) { var r = Unknown("budget line", k, Content.Rules.BudgetStart.Keys); if (r != null) return r; }
                    return null;
                case SetMobilization _: case SecurityOrder _: return null;
                // The GDD orders (#380): ids only. Roles, counts and limits are the sim's, which refuses them with its own
                // reason and refunds the Attention. The legacy content has no cities, so it queues none of these.
                case AssignWorkers w: return Unknown("city", w.City, Content.Cities.Select(x => x.Id));
                case Train t:
                    return Unknown("city", t.City, Content.Cities.Select(x => x.Id))
                        ?? Unknown("training", t.Track, Content.UnitTypes.Where(x => !string.IsNullOrEmpty(x.Facility)).Select(x => x.Id));
                case CancelTraining x:
                    return Unknown("cohort", x.Cohort.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Cohorts.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                // Pioneer work (#378): ids only; the sim refuses a busy unit, a built site or a short Treasury with its own reason.
                case PioneerWork w:
                    return Unknown("unit", w.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                        ?? Unknown("pioneer work", w.Action, new[] { Projects.BuildYishuv }) ?? Unknown("site", w.Site, Content.Sites.Select(x => x.Id));
                case StopWork w: return Unknown("unit", w.Unit.ToString(System.Globalization.CultureInfo.InvariantCulture), State.Units.Keys.Select(k => k.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                default: return text["order.invalid"];
            }
        }

        string? Refuse(string reason) { Refused?.Invoke(reason); return reason; }

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
            var old = QueuedLike(c);
            var left = AttentionLeft + (old != null ? Price(old) : 0);
            if (Price(c) > left + 1e-9)
                return Refuse(text.Format("order.no_attention", Price(c), left));
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
                Changed?.Invoke();
                TurnEnded?.Invoke(failed.Rejected);
                return failed;
            }
            LastBatch = batch; LastBatchDate = date;
            queue.Clear();
            Changed?.Invoke();
            TurnEnded?.Invoke(r.Rejected);
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
