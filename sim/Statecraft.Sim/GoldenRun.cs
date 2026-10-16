using System;
using System.Collections.Generic;
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
        public const string Hash1950 = "ca953c983faa2a85216efdf264bf1da9167c9787a06d21a73c76717dfab0f8b4";

        public static readonly DateTime Until1950 = new DateTime(1950, 1, 1);

        /// <summary>Final hash of Play(content, Until1952): the M3 content range (to 31 December 1951). The GDD game ends on
        /// 15 January 1952 (R-LOOP-004), the first turn on or after Until1952, so this is also the mvp-end run.</summary>
        public const string Hash1952 = "b42380ab7935e4daabd0577fa3df231c474f3ad92f6006af0a05918a46743915";

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
    }
}
