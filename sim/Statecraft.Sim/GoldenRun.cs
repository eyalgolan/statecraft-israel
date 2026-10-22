using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim.Reference;

namespace Statecraft.Sim
{
    /// <summary>The reference playthroughs that pin determinism (docs/determinism.md): real content, seed 1948,
    /// Historical mode, the reference player (D-A7: HistoricalAI's orders, then the GDD orders it states) until the date
    /// reaches the given day or the game ends (spec 2026-10-12 section 3: "the reference player over the full loop"). The constants live in the sim library so
    /// Unity players (Golden check) and the editor tests compare against the same values .NET asserts.</summary>
    public static class GoldenRun
    {
        public const ulong Seed = 1948;

        /// <summary>Final hash of Play(content, Until1950). Changing it is a deliberate rules or content change: the value
        /// is pinned in sim/Statecraft.Sim.Tests/golden/manifest.txt, and STATECRAFT_UPDATE_GOLDEN=1 dotnet test rewrites
        /// both (docs/determinism.md). Say why in the commit message.</summary>
        public const string Hash1950 = "1392f3eb726172c284555b2bcacc0be3f8d9e33d19306dfc3baf0e2c63fe249b";

        public static readonly DateTime Until1950 = new DateTime(1950, 1, 1);

        /// <summary>Final hash of Play(content, Until1952): the M3 content range (to 31 December 1951). The GDD game ends on
        /// 15 January 1952 (R-LOOP-004), the first turn on or after Until1952, so this is also the mvp-end run.</summary>
        public const string Hash1952 = "cc96cd0a8d50a7693300ef887673c7ccfc18dac6d4dcaa9f52ed2ed09062eda5";

        public static readonly DateTime Until1952 = new DateTime(1952, 1, 1);

        /// <summary>Steps with the reference player until Date >= <paramref name="until"/>, the game ends, or 1000 turns pass.</summary>
        public static GameState Play(Content c, DateTime until)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, Seed, Mode.Historical);
            for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
                s = Simulation.Step(c, s, Orders(c, s)).State;
            return s;
        }

        /// <summary>The golden runs' orders for one turn: the reference player's (D-A7). Every copy of the golden loop (the
        /// writer preview, the invariants, the era goldens) takes its orders here, so they stay the run the goldens pin.</summary>
        public static IReadOnlyList<Command> Orders(Content c, GameState s) => ReferencePlayer.Commands(c, s);

        /// <summary>The mvp-end golden: the whole game, to its end (Content.End, R-LOOP-004).</summary>
        public static GameState PlayToEnd(Content c) => Play(c ?? throw new ArgumentNullException(nameof(c)), c.End);

        /// <summary>A fresh start of one era (#292), the run the era goldens pin (EraGolden; golden manifest names
        /// hash_&lt;era&gt;_&lt;year&gt;): the golden seed, Historical mode and <see cref="Orders"/>, as <see cref="Play"/>,
        /// except that the turn that would reach the era's <c>to</c> is not taken (that turn's state already holds the next
        /// era's rules). An era later than the first fails here with a message saying so: NewGame reads only the base
        /// rules' start values. Here, not in the test project, so Unity's golden check plays the same run.</summary>
        public static GameState PlayEra(Content c, string eraId, DateTime until, ulong seed = Seed)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var era = c.Eras.FirstOrDefault(e => e.Id == eraId) ?? throw new ArgumentException($"no era '{eraId}' (known: {string.Join(", ", c.Eras.Select(e => e.Id))})");
            if (era.From != c.Rules.Start)
                throw new NotSupportedException($"era '{eraId}' starts {era.From:yyyy-MM-dd}, but a new game starts {c.Rules.Start:yyyy-MM-dd}: a fresh start of a later era needs that era's start values, which are not read yet (chapters design section 6).");
            if (until <= era.From || until > era.To) throw new ArgumentException($"until {until:yyyy-MM-dd} is outside era '{eraId}' ({era.From:yyyy-MM-dd} to {era.To:yyyy-MM-dd})");
            var s = Simulation.NewGame(c, seed, Mode.Historical);
            for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
            {
                var next = Simulation.Step(c, s, Orders(c, s)).State;
                if (next.Date >= era.To) break;
                s = next;
            }
            return s;
        }

        /// <summary>The smoke5 golden's orders for one turn, what the WebGL ?smoke run's GameRoot.Smoke does: each open event,
        /// in the card's order, answered with its first option while the turn's Attention pays for an answer; the rest go
        /// to their ministers at the turn's end (docs/determinism.md). SliceSmokeTests plays the same policy.</summary>
        public static List<Command> SmokeOrders(Content c, GameState s)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var answers = new List<Command>();
            var left = s.Attention;
            foreach (var e in s.PendingEvents.Select(id => c.Events.First(x => x.Id == id)))
            {
                var cmd = new ChooseEventOption(e.Id, e.Options[0].Id);
                var price = AttentionSystem.Cost(c, s, cmd.Kind);
                if (price > left + 1e-9) break;
                left -= price;
                answers.Add(cmd);
            }
            return answers;
        }

        /// <summary>The smoke5 golden: the golden seed, Historical mode, <paramref name="turns"/> turns of <see cref="SmokeOrders"/>.</summary>
        public static GameState PlaySmoke(Content c, int turns)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, Seed, Mode.Historical);
            for (int turn = 0; turn < turns && s.Date < Until1952 && s.GameOver == null; turn++)
                s = Simulation.Step(c, s, SmokeOrders(c, s)).State;
            return s;
        }
    }
}
