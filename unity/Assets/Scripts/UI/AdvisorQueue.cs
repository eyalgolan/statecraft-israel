#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.UI
{
    /// <summary>The messages waiting to show (spec 4.2): critical first, then the higher priority, then the order the signals
    /// fired. At most one teaching message and one non-critical warning show by themselves a turn; the rest wait for the
    /// next turn or Counsel. A critical message is never held back (spec 6: the advisors never stay silent on a real
    /// threat). Filters by the help level and, in Hebrew, by approval (OQ-4) when it is asked, so a Settings change or a
    /// language switch applies at once. Keeps this game's history for Counsel (spec 7.4); none of it is saved.</summary>
    public sealed class AdvisorQueue
    {
        public enum Outcome { Shown, PutOff, Missed }

        public readonly struct Entry
        {
            public Entry(string id, int turn, DateTime date, Outcome outcome) { Id = id; Turn = turn; Date = date; Outcome = outcome; }
            public string Id { get; }
            public int Turn { get; }
            public DateTime Date { get; }
            public Outcome Outcome { get; }
        }

        sealed class Waiting { public AdvisorCatalog.Message M = null!; public int Seq; public int NotBefore; public bool PutOff; }

        readonly AdvisorCatalog catalog; readonly AdvisorSettings settings;
        readonly List<Waiting> waiting = new List<Waiting>();
        readonly List<Entry> history = new List<Entry>();
        readonly Dictionary<string, int> lastShown = new Dictionary<string, int>(StringComparer.Ordinal);
        int seq, capTurn = int.MinValue, shownThisTurn;
        bool teachingShown, warningShown;

        public AdvisorQueue(AdvisorCatalog catalog, AdvisorSettings settings) { this.catalog = catalog; this.settings = settings; }

        public IReadOnlyList<Entry> History => history;
        public IEnumerable<string> PutOffIds => waiting.Where(w => w.PutOff).Select(w => w.M.Id);

        /// <summary>Queues each message whose signal holds now and whose condition holds, unless it is already waiting, is
        /// filtered out (Eligible) or waits on a message not yet seen.</summary>
        public void Offer(IReadOnlyCollection<string> holding, Func<IReadOnlyDictionary<string, double>, bool> when, int turn)
        {
            foreach (var m in catalog.Messages)
            {
                if (!holding.Contains(m.Signal) || waiting.Any(w => w.M == m)) continue;
                if (!Eligible(m, turn) || !when(m.When)) continue;
                waiting.Add(new Waiting { M = m, Seq = seq++, NotBefore = turn });
            }
        }

        bool Eligible(AdvisorCatalog.Message m, int turn) =>
            Array.IndexOf(AdvisorCatalog.Levels, m.Level) >= settings.LevelRank
            && (m.Repeat == AdvisorCatalog.Once ? !settings.Seen(m.Id) : !(lastShown.TryGetValue(m.Id, out var t) && turn < t + m.CooldownTurns))
            && m.Requires.All(settings.Seen);

        void Caps(int turn)
        {
            if (turn == capTurn) return;
            capTurn = turn; teachingShown = warningShown = false; shownThisTurn = 0;
        }

        bool UnderCap(AdvisorCatalog.Message m) => m.Critical || !(m.Teaching ? teachingShown : warningShown);

        IEnumerable<AdvisorCatalog.Message> Showable(int turn, bool hebrew)
        {
            Caps(turn);
            return waiting.Where(w => w.NotBefore <= turn && Eligible(w.M, turn) && w.M.ApprovedIn(hebrew) && UnderCap(w.M))
                .OrderByDescending(w => w.M.Critical).ThenByDescending(w => w.M.Priority).ThenBy(w => w.Seq).Select(w => w.M);
        }

        public AdvisorCatalog.Message? Head(int turn, bool hebrew) => Showable(turn, hebrew).FirstOrDefault();

        /// <summary>The counter (spec 7.1, "1 of 3"): this one's place among those shown this turn and those still showable.</summary>
        public (int number, int total) Position(int turn, bool hebrew)
        {
            var left = Showable(turn, hebrew).ToList();
            // At most one more teaching and one more warning can follow by themselves, however many wait.
            int more = left.Count(m => m.Critical) + (left.Any(m => !m.Critical && m.Teaching) ? 1 : 0) + (left.Any(m => !m.Critical && !m.Teaching) ? 1 : 0);
            return (shownThisTurn + 1, shownThisTurn + more);
        }

        /// <summary>Got it or the X: seen (once), or its cooldown starts (every); it uses its kind's slot; into the history.</summary>
        public void Done(AdvisorCatalog.Message m, int turn, DateTime date)
        {
            Take(m, turn);
            if (m.Repeat == AdvisorCatalog.Once) settings.MarkSeen(m.Id); else lastShown[m.Id] = turn;
            history.Add(new Entry(m.Id, turn, date, Outcome.Shown));
        }

        /// <summary>Not now: off to the next turn, and into the history as put off. A critical message has no Not now.</summary>
        public void PutOff(AdvisorCatalog.Message m, int turn, DateTime date)
        {
            if (m.Critical) throw new InvalidOperationException($"{m.Id} is critical: it has no Not now");
            Take(m, turn);
            waiting.Add(new Waiting { M = m, Seq = seq++, NotBefore = turn + 1, PutOff = true });
            history.Add(new Entry(m.Id, turn, date, Outcome.PutOff));
        }

        /// <summary>A layer covered it and its signal no longer holds when the layer closed (spec 4.2): into the history,
        /// not shown again now, and not marked seen, so its signal can bring it back later.</summary>
        public void Drop(AdvisorCatalog.Message m, int turn, DateTime date)
        {
            waiting.RemoveAll(w => w.M == m);
            history.Add(new Entry(m.Id, turn, date, Outcome.Missed));
        }

        /// <summary>An event signal stopped being true this turn (the budget submitted): each message waiting on it, shown or not,
        /// goes to the history as missed, not seen, so the signal can bring it back.</summary>
        public void Withdraw(string signal, int turn, DateTime date)
        {
            foreach (var w in waiting.Where(w => w.M.Signal == signal).ToList())
            {
                waiting.Remove(w);
                history.Add(new Entry(w.M.Id, turn, date, Outcome.Missed));
            }
        }

        void Take(AdvisorCatalog.Message m, int turn)
        {
            Caps(turn);
            waiting.RemoveAll(w => w.M == m);
            shownThisTurn++;
            if (m.Critical) return;
            if (m.Teaching) teachingShown = true; else warningShown = true;
        }

        /// <summary>A new or loaded game: nothing waits and the history starts empty (the seen marks stay: they are the player's).</summary>
        public void Reset()
        {
            waiting.Clear(); history.Clear(); lastShown.Clear();
            capTurn = int.MinValue; shownThisTurn = 0; teachingShown = warningShown = false;
        }
    }
}
